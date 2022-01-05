using System.Linq.Expressions;

namespace DomainFixture;

public interface IGreaterThanPropertyBuilder<TProperty, TEntity> :
    IConventionsPropertyBuilder<TProperty, TEntity>, 
    IScenarioBuilder<TProperty, TEntity>
    where TProperty : IComparable, IComparable<TProperty> 
{
    public IGreaterThanScenarioBuilder<TProperty, TEntity> GreaterThan(TProperty value);
    public IGreaterThanScenarioBuilder<TProperty, TEntity> GreaterThanOrEqualTo(TProperty value);
    public IGreaterThanScenarioBuilder<TProperty, TEntity> 
        GreaterThan(Expression<Func<TEntity, TProperty>> func);
    public IGreaterThanScenarioBuilder<TProperty, TEntity> 
        GreaterThanOrEqualTo(Expression<Func<TEntity, TProperty>> func);
}

public class GreaterThanPropertyBuilder<TProperty, TEntity> :
    ConventionsPropertyBuilder<TProperty, TEntity>,
    IGreaterThanPropertyBuilder<TProperty, TEntity>
    where TProperty : struct, IComparable, IComparable<TProperty> 
{
    IList<IScenarioBuilder<TProperty, TEntity, IPropertyBuilder<TProperty, TEntity>>>
        IScenarioBuilder<TProperty, TEntity>.Scenarios { get; } 
        = new List<IScenarioBuilder<TProperty, TEntity, IPropertyBuilder<TProperty, TEntity>>>();

    internal IList<IScenarioBuilder<TProperty, TEntity, IPropertyBuilder<TProperty, TEntity>>> Scenarios
        => ((IScenarioBuilder<TProperty, TEntity>)this).Scenarios;

    public GreaterThanPropertyBuilder(Expression<Func<TEntity, TProperty>> propertyExpression) : base(propertyExpression)
    {
    }

    public IGreaterThanScenarioBuilder<TProperty, TEntity> GreaterThan(TProperty value)
    {
        var scenario = new GreaterThanScenario<TProperty, TEntity>(value, false, PropertyExpression);
        var builder = new GreaterThanScenarioBuilder<TProperty, TEntity>(scenario, this);

        Scenarios.Add(builder);
        
        return builder;
    }

    public IGreaterThanScenarioBuilder<TProperty, TEntity> GreaterThanOrEqualTo(TProperty value)
    {
        var scenario = new GreaterThanScenario<TProperty, TEntity>(value, true, PropertyExpression);
        var builder = new GreaterThanScenarioBuilder<TProperty, TEntity>(scenario, this);

        Scenarios.Add(builder);
        
        return builder;
    }

    public IGreaterThanScenarioBuilder<TProperty, TEntity> GreaterThan(Expression<Func<TEntity, TProperty>> func)
    {
        throw new NotImplementedException();
    }

    public IGreaterThanScenarioBuilder<TProperty, TEntity> GreaterThanOrEqualTo(Expression<Func<TEntity, TProperty>> func)
    {
        throw new NotImplementedException();
    }
}

public interface IGreaterThanScenarioBuilder<TProperty, TEntity> : 
    IScenarioBuilder<TProperty, TEntity>
    where TProperty : IComparable, IComparable<TProperty>
{
}

public class GreaterThanScenarioBuilder<TProperty, TEntity> :
    ScenarioBuilder<
        IGreaterThanPropertyBuilder<TProperty, TEntity> ,
        TProperty, 
        TEntity, 
        GreaterThanScenario<TProperty, TEntity>
    >,
    IGreaterThanScenarioBuilder<TProperty, TEntity> 
    where TProperty : IComparable, IComparable<TProperty> 
{
    public GreaterThanScenarioBuilder(GreaterThanScenario<TProperty, TEntity> scenario, 
        IGreaterThanPropertyBuilder<TProperty, TEntity> builder)
        : base(scenario, builder)
    {
    }

    protected override void ApplyScenario(ScenarioApplier<TProperty, TEntity> applier)
    {
        var value = Scenario.Value;
        var equal = Scenario.Equal;
        var @delegate = equal ? applier.Success.Constant : applier.Failure.Constant;
        
        // Variable delegate due to equal to being an option
        @delegate(value);
        unchecked
        {
            applier.Success.Constant((dynamic)value + 1);
            
            if (equal)
                applier.Failure.Constant((dynamic)value - 1);
        }
    }
}

public record GreaterThanScenario<TProperty, TEntity>(
    TProperty Value,
    bool Equal,
    Expression<Func<TEntity, TProperty>> PropertyExpression
) : IScenario<TProperty, TEntity>;