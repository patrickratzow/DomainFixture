using System.Reflection;
using FluentValidation;

namespace DomainFixture.Tests.Domain;

public abstract class Validatable<TThis>
{
    private static List<IValidator>? _validators;
    private static List<IValidator> Validators
        => _validators ??= Assembly.GetExecutingAssembly()
            .GetExportedTypes()
            .Where(t => t.IsAssignableTo(typeof(IValidator)))
            .Select(t => Activator.CreateInstance(t) as IValidator)
            .Where(t => t is not null)
            .ToList()!;

    protected virtual void Validate()
    {
        var type = GetType();
        ValidateBaseClasses(type);
        Validate(type);
    }

    private void ValidateBaseClasses(Type? type)
    {
        if (type is null) return;
        if (type == typeof(TThis)) return;

        ValidateBaseClasses(type.BaseType);
        Validate(type);
    }

    private void Validate(Type type)
    {
        var validator = FindValidators(type);
        var context = new ValidationContext<object>(this);
        var errors = validator
            .Select(x => x.Validate(context))
            .SelectMany(x => x.Errors)
            .Where(x => x is not null)
            .ToList();

        if (errors.Any()) throw new ValidationException(errors);
    }

    private IEnumerable<IValidator> FindValidators(Type type)
    {
        return Validators
            .Where(x =>
            {
                var interfaces = x.GetType().GetInterfaces();
                var genericTypeArguments = interfaces.Select(i => i.GenericTypeArguments);

                return genericTypeArguments
                    .Any(genericType => genericType.Any(generic => generic == type));
            });
    }
}