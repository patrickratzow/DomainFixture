using System.Linq.Expressions;

namespace DomainFixture;

public interface ILessThanPropertyBuilder<TProperty, TEntity> :
    IConventionsPropertyBuilder<TProperty, TEntity>, 
    IScenarioBuilder<TProperty, TEntity>
    where TProperty : IComparable, IComparable<TProperty> 
{
    public ILessThanScenarioBuilder<TProperty, TEntity> LessThan(TProperty value);
    public ILessThanScenarioBuilder<TProperty, TEntity> LessThanOrEqualTo(TProperty value);
    public ILessThanScenarioBuilder<TProperty, TEntity> LessThan(Expression<Func<TEntity, TProperty>> func);
    public ILessThanScenarioBuilder<TProperty, TEntity> LessThanOrEqualTo(Expression<Func<TEntity, TProperty>> func);
}

public class LessThanPropertyBuilder<TProperty, TEntity> :
    ConventionsPropertyBuilder<TProperty, TEntity>,
    ILessThanPropertyBuilder<TProperty, TEntity>
    where TProperty : IComparable, IComparable<TProperty> 
{
    IList<IScenarioBuilder<TProperty, TEntity, IPropertyBuilder<TProperty, TEntity>>>
        IScenarioBuilder<TProperty, TEntity>.Scenarios { get; } 
        = new List<IScenarioBuilder<TProperty, TEntity, IPropertyBuilder<TProperty, TEntity>>>();

    internal IList<IScenarioBuilder<TProperty, TEntity, IPropertyBuilder<TProperty, TEntity>>> Scenarios
        => ((IScenarioBuilder<TProperty, TEntity>)this).Scenarios;
    
    public LessThanPropertyBuilder(Expression<Func<TEntity, TProperty>> propertyExpression) : base(propertyExpression)
    {
    }

    public ILessThanScenarioBuilder<TProperty, TEntity> LessThan(TProperty value)
    {
        var scenario = new LessThanScenario<TProperty, TEntity>(value, false, PropertyExpression);
        var builder = new LessThanScenarioBuilder<TProperty, TEntity>(scenario, this);

        Scenarios.Add(builder);
        
        return builder;
    }

    public ILessThanScenarioBuilder<TProperty, TEntity> LessThanOrEqualTo(TProperty value)
    {
        var scenario = new LessThanScenario<TProperty, TEntity>(value, true, PropertyExpression);
        var builder = new LessThanScenarioBuilder<TProperty, TEntity>(scenario, this);

        Scenarios.Add(builder);
        
        return builder;
    }

    public ILessThanScenarioBuilder<TProperty, TEntity> LessThan(Expression<Func<TEntity, TProperty>> func)
    {
        throw new NotImplementedException();
    }

    public ILessThanScenarioBuilder<TProperty, TEntity> LessThanOrEqualTo(Expression<Func<TEntity, TProperty>> func)
    {
        throw new NotImplementedException();
    }
}

public interface ILessThanScenarioBuilder<TProperty, TEntity> : 
    IScenarioBuilder<TProperty, TEntity> 
    where TProperty : IComparable, IComparable<TProperty> 
{
}

public class LessThanScenarioBuilder<TProperty, TEntity> :
    ScenarioBuilder<
        ILessThanPropertyBuilder<TProperty, TEntity>,
        TProperty, 
        TEntity, 
        LessThanScenario<TProperty, TEntity>
    >,
    ILessThanScenarioBuilder<TProperty, TEntity> 
    where TProperty : IComparable, IComparable<TProperty> 
{
    public LessThanScenarioBuilder(LessThanScenario<TProperty, TEntity> scenario, 
        ILessThanPropertyBuilder<TProperty, TEntity> builder)
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
            applier.Success.Constant((dynamic)value - 1);
            
            if (equal)
                applier.Failure.Constant((dynamic)value + 1);
        }
    }
}

public record LessThanScenario<TProperty, TEntity>(
    TProperty Value,
    bool Equal,
    Expression<Func<TEntity, TProperty>> PropertyExpression
) : IScenario<TProperty, TEntity>;