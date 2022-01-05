using System.Reflection;

namespace DomainFixture.Conventions;

public class CreatedAtPropertyConvention : IPropertyConvention<DateTime>, IPropertyConvention<DateTimeOffset>
{
    public IGenericPropertyBuilder<DateTime, TEntity> Run<TEntity>(PropertyInfo propertyInfo, 
        IGenericPropertyBuilder<DateTime, TEntity> builder)
        => propertyInfo.Name switch 
        {
            "CreatedAt" => builder.ShouldBeInThePast(),
            "ExpiresAt" => builder.ShouldBeInTheFuture(),
            "UpdatedAt" => builder.ShouldBeInTheFuture(),
            _ => builder
        };

    public IGenericPropertyBuilder<DateTimeOffset, TEntity> Run<TEntity>(PropertyInfo propertyInfo, 
        IGenericPropertyBuilder<DateTimeOffset, TEntity> builder)
        => propertyInfo.Name switch 
        {
            "CreatedAt" => builder.ShouldBeInThePast(),
            "ExpiresAt" => builder.ShouldBeInTheFuture(),
            "UpdatedAt" => builder.ShouldBeInTheFuture(),
            _ => builder
        };
}