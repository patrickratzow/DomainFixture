using System.Reflection;

namespace DomainFixture;

public interface IPropertyConvention
{
}

public interface IPropertyConvention<TProperty> : IPropertyConvention
{
    IGenericPropertyBuilder<TProperty, TEntity> Run<TEntity>(PropertyInfo propertyInfo, 
        IGenericPropertyBuilder<TProperty, TEntity> builder);
}