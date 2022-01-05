using System.Reflection;

namespace DomainFixture.Conventions;

public class BooleanPropertyConvention : IPropertyConvention<bool>
{
    public IGenericPropertyBuilder<bool, TEntity> Run<TEntity>(PropertyInfo propertyInfo, 
        IGenericPropertyBuilder<bool, TEntity> builder)
    {
        if (builder is not ConventionsPropertyBuilder<bool, TEntity> { IsEmpty: true }) return builder;

        return builder.Valid(true, false);
    }
}