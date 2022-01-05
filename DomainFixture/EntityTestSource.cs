using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace DomainFixture;

public static class EntityTestSource
{
    private static dynamic? CreateValidEntity(Type entityType)
    {
        if (!FluentTests.Bases.Any(entityType.IsSubclassOf)) return null;
        var instance = Activator.CreateInstance(entityType, true);
        if (instance is null) return null;
        
        MakeEntityValid(instance, instance.GetType());
        
        return instance;
    }
    
    public static IEnumerable<object[]> Test(Type entityType, Type[] baseTypes)
    {
        foreach (var baseType in baseTypes)
        {
            if (FluentTests.Bases.Contains(baseType)) continue;
        
            FluentTests.Bases.Add(baseType);
        }
        
        if (!FluentTests.TryGetEntityData(entityType, out var entityDataDictionary)) yield break;
        var instance = Activator.CreateInstance(entityType, true);
        if (instance is null) yield break;

        var properties = instance.GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        foreach (var property in properties)
        {
            var key = property.Name;
            if (!entityDataDictionary.TryGetValue(key, out var tuple)) continue;
            var (valids, invalids) = tuple;

            MakeEntityValid(instance, instance.GetType());

            foreach (var valid in valids)
            {
                var value = (dynamic)valid;
                var compiled = value.Compile();
                var valueName = GetValueName(value);
                
                yield return new object[]
                {
                    new EntityTester(instance, key, compiled, true),
                    key,
                    valueName
                };
            }
            foreach (var invalid in invalids)
            {
                var value = (dynamic)invalid;
                var compiled = value.Compile();
                var valueName = GetValueName(value);
                
                yield return new object[]
                {
                    new EntityTester(instance, key, compiled, false),
                    key,
                    valueName
                };
            }
        }
    }

    private static string GetValueName(dynamic value)
    {
        var name = value.Body.ToString();
        if (value.Body is not MemberExpression body) return name;
        var propertyBuilder = body.Expression;
        if (propertyBuilder is not ConstantExpression constantExpression) return name;
        var fieldType = constantExpression.Value?.GetType();
        if (fieldType is null) return name;
        var field = fieldType.GetField("value", BindingFlags.Instance | BindingFlags.Public);
        if (field is null) return name;
                
        return field.GetValue(constantExpression.Value)?.ToString() ?? name;
    }

    private static void MakeEntityValid(object entity, Type? type)
    {
        if (type is null) return;
        if (type == typeof(object) || FluentTests.Bases.Any(x => x == type) || type == typeof(ValueType)) return;

        MakeEntityValid(entity, type.BaseType);
        if (!FluentTests.TryGetEntityData(type, out var entityDataDictionary))
            throw new InvalidOperationException($"{type.Name} has no {nameof(IClassConfiguration)}");
        
        var properties = type
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        
        foreach (var property in properties)
        {
            dynamic value;
            var key = property.Name;
            if (!entityDataDictionary.TryGetValue(key, out var tuple))
            {
                var entityType = property.PropertyType;
                if (property.PropertyType.IsAssignableTo(typeof(IList)))
                {
                    entityType = property.PropertyType.GenericTypeArguments.First();
                }

                if (entityType.IsAbstract && !entityType.IsInterface)
                {
                    entityType = entityType.Assembly
                        .GetTypes()
                        .First(t => t.IsSubclassOf(entityType));
                }

                value = CreateValidEntity(entityType) ?? 
                        throw new InvalidOperationException($"Unable to find tests for property named {key}");
                
                if (property.PropertyType.IsAssignableTo(typeof(IList)))
                {
                    var constructedType = typeof(List<>).MakeGenericType(entityType);
                    var collection = Activator.CreateInstance(constructedType);

                    (collection as IList)!.Add(value);
                    value = collection;
                }
            }
            else
            {
                var operation = tuple.Valid.FirstOrDefault();
                if (operation is null)
                    throw new InvalidOperationException(
                        $"Unable to find any valid values for {entity.GetType().Name}. " +
                        "Cannot create a valid instance without knowing what is valid");
                
                var compiled = ((dynamic)operation).Compile();
                value = compiled.Invoke((dynamic)entity);
            }
            
            property.SetValue(entity, value);
        }
    }
}

public interface IEntityTester
{
    void Run();
}

public class EntityTester : IEntityTester
{
    private readonly object _entity;
    private readonly string _propertyName;
    private readonly dynamic _compiledFunc;
    private readonly bool _shouldSucceed;

    public EntityTester(object entity, string propertyName, dynamic compiledFunc, bool shouldSucceed)
    {
        _entity = entity;
        _propertyName = propertyName;
        _compiledFunc = compiledFunc;
        _shouldSucceed = shouldSucceed;
    }

    public void Run()
    {
        var type = _entity.GetType();
        var property = type.GetProperty(_propertyName, BindingFlags.Instance | BindingFlags.Public);
        if (property is null)
            throw new MissingMemberException($"Expected {type.Name} to have a {_propertyName} property");
        var value = property.GetValue(_entity);

        try
        {
            var newValue = _compiledFunc((dynamic)_entity);
            property.SetValue(_entity, newValue);

            var validateMethod = type.GetMethod("Validate", BindingFlags.Instance | BindingFlags.NonPublic);
            validateMethod?.Invoke(_entity, Array.Empty<object>());

            if (!_shouldSucceed)
                throw new Exception("Excepted to fail :(");
        }
        catch (TargetInvocationException exception) //when (exception.InnerException is ValidationException)
        {
            if (_shouldSucceed)
                throw;
        }
        finally
        {
            property.SetValue(_entity, value);
        }
    }

    public override string ToString()
    {
        return $"{nameof(EntityTester)}<{_entity.GetType().Name}>";
    }
}