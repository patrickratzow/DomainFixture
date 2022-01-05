using System.Reflection;

namespace DomainFixture;

public interface IEnumPropertyConvention : IPropertyConvention
{
    void Run<TEntity>(PropertyInfo propertyInfo, IGenericPropertyBuilder<object, TEntity> builder);
}