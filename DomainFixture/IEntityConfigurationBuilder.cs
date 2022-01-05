using System.Linq.Expressions;

namespace DomainFixture;

public interface IEntityConfigurationBuilder
{
}

public interface IEntityConfigurationBuilder<TEntity> : IEntityConfigurationBuilder where TEntity : class
{
    IConventionsPropertyBuilder<TProperty, TEntity> Property<TProperty>(
        Expression<Func<TEntity, TProperty>> propertyExpression);
}

public class EntityConfigurationBuilder : IEntityConfigurationBuilder
{
    public List<IPropertyBuilder> PropertyBuilders { get; } = new();
}

public class EntityConfigurationBuilder<TEntity> : EntityConfigurationBuilder, 
    IEntityConfigurationBuilder<TEntity> where TEntity : class
{
    public IConventionsPropertyBuilder<TProperty, TEntity> Property<TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression)
    {
        var builder = new ConventionsPropertyBuilder<TProperty, TEntity>(propertyExpression);

        PropertyBuilders.Add(builder);

        return builder;
    }
}