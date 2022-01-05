using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using DomainFixture.Setup.Conventions;

namespace DomainFixture;

public static class FluentTests
{
    private static readonly Dictionary<Type, EntityDataDictionary> EntityConfigDictionary = new();
    internal static readonly HashSet<Type> Bases = new();
    internal static Assembly Assembly { get; private set; } = null!;
    public static void AddBase(Type type) => Bases.Add(type);
    public static void AddAssembly(Assembly assembly) => Assembly = assembly;
    
    internal static bool TryGetEntityData(Type entityType, out EntityDataDictionary entityDataDictionary)
    {
        ScanEntityConfigurations(entityType);

        return RecursiveEntityDataDictionary(entityType, out entityDataDictionary);
    }

    private static bool RecursiveEntityDataDictionary(Type? entityType, out EntityDataDictionary entityData)
    {
        entityData = new();

        while (entityType is not null && entityType != typeof(object) && Bases.All(x => x != entityType))
        {
            if (!EntityConfigDictionary.TryGetValue(entityType, out var entityDataDictionary))
                return false;

            foreach (var tuple in entityDataDictionary!)
            {
                entityData[tuple.Key] = tuple.Value;
            }
            
            entityType = entityType.BaseType;
        }

        return true;
    }
    
    private static void ScanEntityConfigurations(Type entityType)
    {
        if (EntityConfigDictionary.Any()) return;
        
        var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes());
        var configurations = types
            .Where(t => !t.IsInterface && !t.IsAbstract)
            .Where(t => t.GetInterfaces().Any(x => x.IsDerivedOfGenericType(typeof(IClassConfiguration<>))))
            .ToList();

        foreach (var configuration in configurations)
        {
            var configInterface = configuration.GetInterfaces()
                .First(x => x.IsDerivedOfGenericType(typeof(IClassConfiguration<>)));
            var type = configInterface.GenericTypeArguments
                .Where(t => !t.IsInterface)
                .First(t => Bases.Any(t.IsAssignableTo));
            
            if (Activator.CreateInstance(configuration) is not IClassConfiguration config) continue;
            config.Configure();

            // TODO: I'm not very comfortable with the use of dynamic... but it works, for now...
            var entityBuilder = ((dynamic)config).ConfigurationBuilder;
            ApplyPropertyConventions(config, entityBuilder, type);

            var entityData = GetEntityData(entityBuilder);
            if (entityData is null) continue;

            EntityConfigDictionary.Add(type, entityData);
        }
    }

    private static Dictionary<Type, List<IPropertyConvention>>? _conventions;
    private static void ApplyPropertyConventions(IClassConfiguration config, EntityConfigurationBuilder entityBuilder, 
        Type entityType)
    {
        ScanConventions();
        
        if (_scanners is null) throw new NullReferenceException($"{nameof(_scanners)} shouldn't be null");
        
        var properties = entityType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        foreach (var property in properties)
        {
            var applyPropertyConvention = new ApplyPropertyConvention(config, entityBuilder, entityType, _conventions!, 
                property);
            
            foreach (var scanner in _scanners)
            {
                scanner.ApplyConvention(applyPropertyConvention);
            }
        }
        
    }
    

    private static List<IPropertyConventionScanner>? _scanners;

    private static void ScanConventions()
    {
        if (_conventions is not null) return;
        
        _conventions = new();

        var assemblyTypes = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => !t.IsInterface && !t.IsAbstract)
            .ToList();

        _scanners = assemblyTypes
            .Where(t => t.IsAssignableTo(typeof(IPropertyConventionScanner)))
            .Select(Activator.CreateInstance)
            .Cast<IPropertyConventionScanner>()
            .ToList();

        var conventions = _scanners
            .Select(scanner => scanner.GetConventions(assemblyTypes))
            .SelectMany(conventionDictionary => conventionDictionary);

        foreach (var convention in conventions)
        {
            if (!_conventions.TryGetValue(convention.Key, out var list))
                list = new();
                
            list!.AddRange(convention.Value);

            _conventions[convention.Key] = list;
        }
    }
    

    // TODO: This needs to use less Reflection, very dangerous code
    private static EntityDataDictionary? GetEntityData(EntityConfigurationBuilder entityBuilder)
    {
        var entityDataDictionary = new EntityDataDictionary();
        foreach (var propertyBuilder in entityBuilder.PropertyBuilders)
        {
            var validExpressions = GetValidExpressions(propertyBuilder);
            if (validExpressions is null) return null;
            var invalidExpressions = GetInvalidExpressions(propertyBuilder);
            if (invalidExpressions is null) return null;
            var propertyName = GetPropertyName(propertyBuilder);
            if (propertyName is null) return null;

            entityDataDictionary[propertyName] = (validExpressions, invalidExpressions);
        }

        return entityDataDictionary;
    }

    private static List<dynamic>? GetValidExpressions(IPropertyBuilder propertyBuilder)
    {
        var type = propertyBuilder.GetType();
        var validExpressionsProperty = type
            .GetProperty("ValidExpressions", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.GetValue(propertyBuilder);
        
        return validExpressionsProperty is not ICollection validExpressions ? null : GetExpressions(validExpressions);
    }
    
    private static List<dynamic>? GetInvalidExpressions(IPropertyBuilder propertyBuilder)
    {
        var type = propertyBuilder.GetType();
        var invalidExpressionsProperty = type
            .GetProperty("InvalidExpressions", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.GetValue(propertyBuilder);
        
        return invalidExpressionsProperty is not ICollection invalidExpressions ? null : GetExpressions(invalidExpressions);
    }

    private static string? GetPropertyName(IPropertyBuilder propertyBuilder)
    {
        var type = propertyBuilder.GetType();
        var validExpressionsProperty = type
            .GetProperty("PropertyExpression", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.GetValue(propertyBuilder);

        var expression = validExpressionsProperty as dynamic;
        if (expression is null) return null;
        if (expression.Body is ParameterExpression parameterExpression)
            return string.Join(".", parameterExpression.Name!.Split('.').Skip(1));
        
        return expression.Body.Member.Name;
    }

    private static List<dynamic> GetExpressions(IEnumerable collection) 
        => collection.Cast<dynamic>().ToList();
}