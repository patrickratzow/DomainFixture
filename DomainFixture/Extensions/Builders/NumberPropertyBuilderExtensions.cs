using System.Linq.Expressions;

namespace DomainFixture;

public abstract partial class AbstractClassConfiguration<TEntity> where TEntity : class
{
    // Integers
    public IComparablePropertyBuilder<byte, TEntity> Property(Expression<Func<TEntity, byte>> propertyExpression)
        => ComparablePropertyBuilder(propertyExpression);
    public IComparablePropertyBuilder<sbyte, TEntity> Property(Expression<Func<TEntity, sbyte>> propertyExpression)
        => ComparablePropertyBuilder(propertyExpression);
    public IComparablePropertyBuilder<short, TEntity> Property(Expression<Func<TEntity, short>> propertyExpression)
        => ComparablePropertyBuilder(propertyExpression);
    public IComparablePropertyBuilder<ushort, TEntity> Property(Expression<Func<TEntity, ushort>> propertyExpression)
        => ComparablePropertyBuilder(propertyExpression);
    public IComparablePropertyBuilder<int, TEntity> Property(Expression<Func<TEntity, int>> propertyExpression)
        => ComparablePropertyBuilder(propertyExpression);
    public IComparablePropertyBuilder<uint, TEntity> Property(Expression<Func<TEntity, uint>> propertyExpression)
        => ComparablePropertyBuilder(propertyExpression);
    public IComparablePropertyBuilder<long, TEntity> Property(Expression<Func<TEntity, long>> propertyExpression)
        => ComparablePropertyBuilder(propertyExpression);
    public IComparablePropertyBuilder<ulong, TEntity> Property(Expression<Func<TEntity, ulong>> propertyExpression)
        => ComparablePropertyBuilder(propertyExpression);
    
    // Decimals
    public IComparablePropertyBuilder<float, TEntity> Property(Expression<Func<TEntity, float>> propertyExpression)
        => ComparablePropertyBuilder(propertyExpression);
    public IComparablePropertyBuilder<double, TEntity> Property(Expression<Func<TEntity, double>> propertyExpression)
        => ComparablePropertyBuilder(propertyExpression);
    public IComparablePropertyBuilder<decimal, TEntity> Property(Expression<Func<TEntity, decimal>> propertyExpression)
        => ComparablePropertyBuilder(propertyExpression);
    
    private IComparablePropertyBuilder<TProperty, TEntity> ComparablePropertyBuilder<TProperty>(
        Expression<Func<TEntity, TProperty>> propertyExpression) 
        where TProperty : struct, IComparable, IComparable<TProperty>, IConvertible, IEquatable<TProperty>, IFormattable
    {
        if (ConfigurationBuilder is not EntityConfigurationBuilder<TEntity> entityBuilder)
            throw new InvalidCastException($"Unable to cast to {nameof(EntityConfigurationBuilder<TEntity>)}");

        var builder = new ComparablePropertyBuilder<TProperty, TEntity>(propertyExpression);

        entityBuilder.PropertyBuilders.Add(builder);

        return builder;
    }
}


public class ComparablePropertyBuilder<TProperty, TEntity> : 
    ConventionsPropertyBuilder<TProperty, TEntity>,
    IComparablePropertyBuilder<TProperty, TEntity>
    where TProperty : struct, 
        IComparable, 
        IComparable<TProperty>, 
        IConvertible, 
        IEquatable<TProperty>, 
        IFormattable
{
    private readonly LessThanPropertyBuilder<TProperty, TEntity> _lessThanBuilder;
    private readonly EqualsPropertyBuilder<TProperty, TEntity> _equalsBuilder;
    private readonly GreaterThanPropertyBuilder<TProperty, TEntity> _greaterThanBuilder;

    IList<IScenarioBuilder<TProperty, TEntity, IPropertyBuilder<TProperty, TEntity>>>
        IScenarioBuilder<TProperty, TEntity>.Scenarios 
        => _lessThanBuilder.Scenarios
            .Concat(_equalsBuilder.Scenarios)
            .Concat(_greaterThanBuilder.Scenarios)
            .ToList();

    internal override List<Expression<Func<TEntity, TProperty>>> ValidExpressions
        => _lessThanBuilder.ValidExpressions
            .Concat(_equalsBuilder.ValidExpressions)
            .Concat(_greaterThanBuilder.ValidExpressions)
            .ToList();
    internal override List<Expression<Func<TEntity, TProperty>>> InvalidExpressions
        => _lessThanBuilder.InvalidExpressions
            .Concat(_equalsBuilder.InvalidExpressions)
            .Concat(_greaterThanBuilder.InvalidExpressions)
            .ToList();

    internal override bool IsEmpty => _lessThanBuilder.IsEmpty || _equalsBuilder.IsEmpty || _greaterThanBuilder.IsEmpty;

    public ComparablePropertyBuilder(Expression<Func<TEntity, TProperty>> propertyExpression) : base(propertyExpression)
    {
        _lessThanBuilder = new(propertyExpression);
        _equalsBuilder = new(propertyExpression);
        _greaterThanBuilder = new(propertyExpression);
    }

    public IComparableScenarioBuilder<TProperty, TEntity> LessThan(TProperty value)
    {
        var scenario = new ComparableScenario<TProperty, TEntity>(this);
        var builder = new ComparableScenarioBuilder<TProperty, TEntity>(scenario, this);
        scenario.LessThanScenarioBuilder = new(new(value, false, PropertyExpression), _lessThanBuilder);
        
        return builder;
    }

    public IComparableScenarioBuilder<TProperty, TEntity> LessThanOrEquals(TProperty value)
    {
        var scenario = new ComparableScenario<TProperty, TEntity>(this);
        var builder = new ComparableScenarioBuilder<TProperty, TEntity>(scenario, this);
        scenario.LessThanScenarioBuilder = new(new(value, true, PropertyExpression), _lessThanBuilder);
        
        return builder;
    }

    public IComparableScenarioBuilder<TProperty, TEntity> Equals(TProperty value)
    {
        var scenario = new ComparableScenario<TProperty, TEntity>(this);
        var builder = new ComparableScenarioBuilder<TProperty, TEntity>(scenario, this);
        scenario.EqualsScenarioBuilder = new(new(value, PropertyExpression), _equalsBuilder);

        return builder;
    }

    public IComparableScenarioBuilder<TProperty, TEntity> GreaterThan(TProperty value)
    {
        var scenario = new ComparableScenario<TProperty, TEntity>(this);
        var builder = new ComparableScenarioBuilder<TProperty, TEntity>(scenario, this);
        scenario.GreaterThanScenarioBuilder = new(new(value, false, PropertyExpression), _greaterThanBuilder);
        
        return builder;
    }

    public IComparableScenarioBuilder<TProperty, TEntity> GreaterThanOrEquals(TProperty value)
    {
        var scenario = new ComparableScenario<TProperty, TEntity>(this);
        var builder = new ComparableScenarioBuilder<TProperty, TEntity>(scenario, this);
        scenario.GreaterThanScenarioBuilder = new(new(value, true, PropertyExpression), _greaterThanBuilder);
        
        return builder;
    }

    public IComparableScenarioBuilder<TProperty, TEntity> InclusiveBetween(TProperty lower, TProperty upper)
    {
        var scenario = new ComparableScenario<TProperty, TEntity>(this);
        var builder = new ComparableScenarioBuilder<TProperty, TEntity>(scenario, this);
        scenario.GreaterThanScenarioBuilder = new(new(lower, true, PropertyExpression), _greaterThanBuilder);
        scenario.LessThanScenarioBuilder = new(new(upper, true, PropertyExpression), _lessThanBuilder);
        
        return builder;
    }

    public IComparableScenarioBuilder<TProperty, TEntity> ExclusiveBetween(TProperty lower, TProperty upper)
    {
        var scenario = new ComparableScenario<TProperty, TEntity>(this);
        var builder = new ComparableScenarioBuilder<TProperty, TEntity>(scenario, this);
        scenario.GreaterThanScenarioBuilder = new(new(lower, false, PropertyExpression), _greaterThanBuilder);
        scenario.LessThanScenarioBuilder = new(new(upper, false, PropertyExpression), _lessThanBuilder);
        
        return builder;
    }
}


public interface IComparablePropertyBuilder<TProperty, TEntity> :
    IConventionsPropertyBuilder<TProperty, TEntity>,
    IScenarioBuilder<TProperty, TEntity> 
    where TProperty : struct, 
        IComparable, 
        IComparable<TProperty>, 
        IConvertible, 
        IEquatable<TProperty>, 
        IFormattable
{
    public IComparableScenarioBuilder<TProperty, TEntity> LessThan(TProperty value);
    public IComparableScenarioBuilder<TProperty, TEntity> LessThanOrEquals(TProperty value);
    public IComparableScenarioBuilder<TProperty, TEntity> Equals(TProperty value);
    public IComparableScenarioBuilder<TProperty, TEntity> GreaterThan(TProperty value);
    public IComparableScenarioBuilder<TProperty, TEntity> GreaterThanOrEquals(TProperty value);
    public IComparableScenarioBuilder<TProperty, TEntity> InclusiveBetween(TProperty lower, TProperty upper);
    public IComparableScenarioBuilder<TProperty, TEntity> ExclusiveBetween(TProperty lower, TProperty upper);
}

public interface IComparableScenarioBuilder<TProperty, TEntity> :
    IScenarioBuilder<TProperty, TEntity, IComparablePropertyBuilder<TProperty, TEntity>> 
    where TProperty : struct, 
        IComparable, 
        IComparable<TProperty>, 
        IConvertible, 
        IEquatable<TProperty>, 
        IFormattable
{
}

public class ComparableScenarioBuilder<TProperty, TEntity> :
    ScenarioBuilder<
        IComparablePropertyBuilder<TProperty, TEntity>,
        TProperty, 
        TEntity, 
        ComparableScenario<TProperty, TEntity>
    >,
    IComparableScenarioBuilder<TProperty, TEntity> 
    where TProperty : struct, IComparable, IComparable<TProperty>, IConvertible, IEquatable<TProperty>, IFormattable
{
    public ComparableScenarioBuilder(ComparableScenario<TProperty, TEntity> scenario, 
        IComparablePropertyBuilder<TProperty, TEntity> builder)
        : base(scenario, builder)
    {
    }

    protected override void ApplyScenario(ScenarioApplier<TProperty, TEntity> applier)
    {
    }

    public override IComparablePropertyBuilder<TProperty, TEntity> IsValid()
    {
        Scenario.LessThanScenarioBuilder?.IsValid();
        Scenario.EqualsScenarioBuilder?.IsValid();
        Scenario.GreaterThanScenarioBuilder?.IsValid();

        return Builder;
    }

    public override IComparablePropertyBuilder<TProperty, TEntity> IsInvalid()
    {
        Scenario.LessThanScenarioBuilder?.IsInvalid();
        Scenario.EqualsScenarioBuilder?.IsInvalid();
        Scenario.GreaterThanScenarioBuilder?.IsInvalid();

        return Builder;
    }
}

public class ComparableScenario<TProperty, TEntity> : IScenario<TProperty, TEntity> 
    where TProperty : struct, IComparable, IComparable<TProperty>, IConvertible, IEquatable<TProperty>, IFormattable
{
    public ComparableScenario(IComparablePropertyBuilder<TProperty, TEntity> propertyBuilder,
        LessThanScenarioBuilder<TProperty, TEntity>? lessThanScenarioBuilder = null,
        EqualsScenarioBuilder<TProperty, TEntity>? equalsScenarioBuilder = null,
        GreaterThanScenarioBuilder<TProperty, TEntity>? greaterThanScenarioBuilder = null)
    {
        PropertyBuilder = propertyBuilder;
        LessThanScenarioBuilder = lessThanScenarioBuilder;
        EqualsScenarioBuilder = equalsScenarioBuilder;
        GreaterThanScenarioBuilder = greaterThanScenarioBuilder;
    }

    public IComparablePropertyBuilder<TProperty, TEntity> PropertyBuilder { get; }
    public LessThanScenarioBuilder<TProperty, TEntity>? LessThanScenarioBuilder { get; set; }
    public EqualsScenarioBuilder<TProperty, TEntity>? EqualsScenarioBuilder { get; set; }
    public GreaterThanScenarioBuilder<TProperty, TEntity>? GreaterThanScenarioBuilder { get; set; }
}