using System.Reflection;

namespace DomainFixture.Conventions;

public class EnumPropertyConvention : IEnumPropertyConvention
{
    public void Run<TEntity>(PropertyInfo propertyInfo, IGenericPropertyBuilder<object, TEntity> builder)
    {
        if (builder is not ConventionsPropertyBuilder<object, TEntity> { IsEmpty: true }) return;
        var propertyType = propertyInfo.PropertyType;
        if (!propertyType.IsEnum) return;
        // TODO: Don't wanna deal with the insane complexity of flags... let them do it manually for now
        if (HasFlagAttribute(propertyType)) return;
        
        var values = Enum.GetValues(propertyType).Cast<object>().ToList();
        if (values.Count == 0) return;
        
        foreach (var value in values)
        {
            builder.Valid(value);
        }
        
        unchecked
        {
            var max = values.Max()!;
            var min = values.Min()!;

            builder.Invalid((dynamic)max + 1);
            builder.Invalid((dynamic)min - 1);
        }
    }

    private static bool HasFlagAttribute(MemberInfo type) => type.GetCustomAttributes<FlagsAttribute>().Any();
}