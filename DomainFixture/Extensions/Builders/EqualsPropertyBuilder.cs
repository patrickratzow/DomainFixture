using System.Linq.Expressions;

namespace DomainFixture;

public interface IEqualsPropertyBuilder<TProperty, TEntity> :
    IConventionsPropertyBuilder<TProperty, TEntity>, 
    IScenarioBuilder<TProperty, TEntity>
    where TProperty : IComparable, IComparable<TProperty>
{
    public IEqualsScenarioBuilder<TProperty, TEntity> Equals(TProperty value);
    public IEqualsScenarioBuilder<TProperty, TEntity> Equals(Expression<Func<TEntity, TProperty>> func);
}

public class EqualsPropertyBuilder<TProperty, TEntity> :
    ConventionsPropertyBuilder<TProperty, TEntity>,
    IEqualsPropertyBuilder<TProperty, TEntity>
    where TProperty : struct, IComparable, IComparable<TProperty>
{
    IList<IScenarioBuilder<TProperty, TEntity, IPropertyBuilder<TProperty, TEntity>>>
        IScenarioBuilder<TProperty, TEntity>.Scenarios { get; } 
        = new List<IScenarioBuilder<TProperty, TEntity, IPropertyBuilder<TProperty, TEntity>>>();

    internal IList<IScenarioBuilder<TProperty, TEntity, IPropertyBuilder<TProperty, TEntity>>> Scenarios
        => ((IScenarioBuilder<TProperty, TEntity>)this).Scenarios;

    public EqualsPropertyBuilder(Expression<Func<TEntity, TProperty>> propertyExpression) : base(propertyExpression)
    {
    }

    public IEqualsScenarioBuilder<TProperty, TEntity> Equals(TProperty value)
    {
        var scenario = new EqualsScenario<TProperty, TEntity>(value, PropertyExpression);
        var builder = new EqualsScenarioBuilder<TProperty, TEntity>(scenario, this);

        Scenarios.Add(builder);
        
        return builder;
    }

    public IEqualsScenarioBuilder<TProperty, TEntity> Equals(Expression<Func<TEntity, TProperty>> func)
    {
        throw new NotImplementedException();
    }
}

public interface IEqualsScenarioBuilder<TProperty, TEntity> : 
    IScenarioBuilder<TProperty, TEntity, IEqualsPropertyBuilder<TProperty, TEntity>> 
    where TProperty : IComparable, IComparable<TProperty>
{
}

public class EqualsScenarioBuilder<TProperty, TEntity> :
    ScenarioBuilder<
        IEqualsPropertyBuilder<TProperty, TEntity>, 
        TProperty, 
        TEntity, 
        EqualsScenario<TProperty, TEntity>
    >,
    IEqualsScenarioBuilder<TProperty, TEntity> 
    where TProperty : IComparable, IComparable<TProperty>
{
    public EqualsScenarioBuilder(EqualsScenario<TProperty, TEntity> scenario,
        IEqualsPropertyBuilder<TProperty, TEntity> builder)
        : base(scenario, builder)
    {
    }

    protected override void ApplyScenario(ScenarioApplier<TProperty, TEntity> applier)
    {
        var value = Scenario.Value;
        
        applier.Success.Constant(value);
    }
}

public record EqualsScenario<TProperty, TEntity>(
    TProperty Value,
    Expression<Func<TEntity, TProperty>> PropertyExpression
) : IScenario<TProperty, TEntity>;