using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace DomainFixture;

public interface IPropertyBuilder
{
}

[SuppressMessage("ReSharper", "UnusedTypeParameter")]
public interface IPropertyBuilder<TProperty, TEntity> : IPropertyBuilder
{
}

public interface IGenericPropertyBuilder<TProperty, TEntity> : IPropertyBuilder<TProperty, TEntity>
{
    IGenericPropertyBuilder<TProperty, TEntity> Valid(params TProperty[] values);
    IGenericPropertyBuilder<TProperty, TEntity> Invalid(params TProperty[] values);
    IGenericPropertyBuilder<TProperty, TEntity> Valid(Expression<Func<TEntity, TProperty>> expression);
    IGenericPropertyBuilder<TProperty, TEntity> Invalid(Expression<Func<TEntity, TProperty>> expression);
}

public interface IConventionsPropertyBuilder<TProperty, TEntity> : IGenericPropertyBuilder<TProperty, TEntity>
{
    public IConventionsPropertyBuilder<TProperty, TEntity> With<TConvention>() 
        where TConvention : IPropertyConvention<TProperty>;
    public IConventionsPropertyBuilder<TProperty, TEntity> Without<TConvention>() 
        where TConvention : IPropertyConvention<TProperty>;
}