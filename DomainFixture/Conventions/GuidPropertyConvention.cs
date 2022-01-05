using System.Reflection;

namespace DomainFixture.Conventions;

public class GuidPropertyConvention : IPropertyConvention<Guid>
{
    public IGenericPropertyBuilder<Guid, TEntity> Run<TEntity>(PropertyInfo propertyInfo,
        IGenericPropertyBuilder<Guid, TEntity> builder)
        => builder.ShouldBeGuid();
}