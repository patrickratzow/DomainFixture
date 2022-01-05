using System.Linq.Expressions;

namespace DomainFixture;

public class ConventionsPropertyBuilder<TProperty, TEntity> : IConventionsPropertyBuilder<TProperty, TEntity>
{
    internal virtual List<Expression<Func<TEntity, TProperty>>> ValidExpressions { get; } = new();
    internal virtual List<Expression<Func<TEntity, TProperty>>> InvalidExpressions { get; } = new();
    internal Expression<Func<TEntity, TProperty>> PropertyExpression { get; }
    internal IClassConventionsConfiguration Conventions { get; } = new ClassConventionsConfiguration();
    internal virtual bool IsEmpty => ValidExpressions.Count == 0 && InvalidExpressions.Count == 0;

    public ConventionsPropertyBuilder(Expression<Func<TEntity, TProperty>> propertyExpression)
    {
        PropertyExpression = propertyExpression;
    }

    public IGenericPropertyBuilder<TProperty, TEntity> Valid(params TProperty[] values)
    {
        foreach (var value in values)
        {
            Valid(_ => value);
        }
        
        return this;
    }
    
    public IGenericPropertyBuilder<TProperty, TEntity> Invalid(params TProperty[] values)
    {
        foreach (var value in values)
        {
            Invalid(_ => value);
        }
        
        return this;
    }

    public IGenericPropertyBuilder<TProperty, TEntity> Valid(Expression<Func<TEntity, TProperty>> func)
    {
        ValidExpressions.Add(func);

        return this;
    }

    public IGenericPropertyBuilder<TProperty, TEntity> Invalid(Expression<Func<TEntity, TProperty>> func)
    { 
        InvalidExpressions.Add(func);

        return this;
    }
    
    public IConventionsPropertyBuilder<TProperty, TEntity> With<TConvention>() where TConvention : IPropertyConvention<TProperty>
    {
        Conventions.Add<TConvention>();
        
        return this;
    }
    
    public IConventionsPropertyBuilder<TProperty, TEntity> Without<TConvention>() where TConvention : IPropertyConvention<TProperty>
    {
        Conventions.Remove<TConvention>();
        
        return this;
    }
}