using System.Linq.Expressions;

namespace DomainFixture;

public interface IClassConfiguration
{
    IClassConventionsConfiguration Conventions { get; }
    void Configure();
}

public interface IClassConfiguration<TEntity> : IClassConfiguration where TEntity : class
{
    public IEntityConfigurationBuilder<TEntity> ConfigurationBuilder { get; }
}

public interface IClassConventionsConfiguration
{
    HashSet<IPropertyConvention> AddedConventions { get; }
    HashSet<IPropertyConvention> RemovedConventions { get; }
    IClassConventionsConfiguration Add<TConvention>() where TConvention : IPropertyConvention;
    IClassConventionsConfiguration Add(Type type);
    IClassConventionsConfiguration Remove<TConvention>() where TConvention : IPropertyConvention;
    IClassConventionsConfiguration Remove(Type type);
}

public class ClassConventionsConfiguration : IClassConventionsConfiguration
{
    public HashSet<IPropertyConvention> AddedConventions { get; } = new();
    public HashSet<IPropertyConvention> RemovedConventions { get; } = new();

    public IClassConventionsConfiguration Add<TConvention>() where TConvention : IPropertyConvention
    {
        var instance = Activator.CreateInstance<TConvention>();

        AddedConventions.Add(instance);

        return this;
    }

    public IClassConventionsConfiguration Add(Type type)
    {
        if (Activator.CreateInstance(type) is not IPropertyConvention instance) 
            throw new InvalidOperationException($"{type.Name} is not an property convention");
        
        AddedConventions.Add(instance);

        return this;
    }

    public IClassConventionsConfiguration Remove<TConvention>() where TConvention : IPropertyConvention
    {
        var instance = Activator.CreateInstance<TConvention>();

        RemovedConventions.Add(instance);

        return this;
    }

    public IClassConventionsConfiguration Remove(Type type)
    {
        if (Activator.CreateInstance(type) is not IPropertyConvention instance) 
            throw new InvalidOperationException($"{type.Name} is not an property convention");
        
        RemovedConventions.Add(instance);

        return this;
    }
}

public abstract partial class AbstractClassConfiguration<TEntity> : IClassConfiguration<TEntity> where TEntity : class
{
    public IEntityConfigurationBuilder<TEntity> ConfigurationBuilder { get; } 
        = new EntityConfigurationBuilder<TEntity>();
    public IClassConventionsConfiguration Conventions { get; } 
        = new ClassConventionsConfiguration();

    public IConventionsPropertyBuilder<TProperty, TEntity> Property<TProperty>(
        Expression<Func<TEntity, TProperty>> propertyExpression
    ) => ConfigurationBuilder.Property(propertyExpression);

    public abstract void Configure();
}