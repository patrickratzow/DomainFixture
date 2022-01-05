using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace DomainFixture;

public abstract partial class AbstractClassConfiguration<TEntity> where TEntity : class
{
    public IStringPropertyBuilder<TEntity> Property(Expression<Func<TEntity, string>> propertyExpression)
    {
        if (ConfigurationBuilder is not EntityConfigurationBuilder<TEntity> entityBuilder)
            throw new InvalidCastException($"Unable to cast to {nameof(EntityConfigurationBuilder<TEntity>)}");

        var builder = new StringPropertyBuilder<TEntity>(propertyExpression);

        entityBuilder.PropertyBuilders.Add(builder);

        return builder;
    }
}

public interface IStringPropertyBuilder<TEntity> : 
    IConventionsPropertyBuilder<string, TEntity>, 
    IScenarioBuilder<string, TEntity>
{
    public IStringScenarioBuilder<TEntity> Length(int start, int end);
    public IStringScenarioBuilder<TEntity> Length(int length);
    public IStringScenarioBuilder<TEntity> Empty();
    public IStringScenarioBuilder<TEntity> NotEmpty();
}

public class StringPropertyBuilder<TEntity> : 
    ConventionsPropertyBuilder<string, TEntity>,
    IStringPropertyBuilder<TEntity>
{
    [SuppressMessage("ReSharper", "UnassignedGetOnlyAutoProperty")]
    IList<IScenarioBuilder<string, TEntity, IPropertyBuilder<string, TEntity>>>
        IScenarioBuilder<string, TEntity>.Scenarios { get; } 
        = new List<IScenarioBuilder<string, TEntity, IPropertyBuilder<string, TEntity>>>();
    private IList<IScenarioBuilder<string, TEntity, IPropertyBuilder<string, TEntity>>> Scenarios 
        => ((IScenarioBuilder<string, TEntity>)this).Scenarios;

    public StringPropertyBuilder(Expression<Func<TEntity, string>> propertyExpression) : base(propertyExpression)
    {
    }

    public IStringScenarioBuilder<TEntity> Length(int start, int end)
    {
        var scenario = new StringScenario<TEntity>(start, end);
        var builder = new StringScenarioBuilder<TEntity>(scenario, this);

        Scenarios.Add(builder);
        
        return builder;
    }

    public IStringScenarioBuilder<TEntity> Length(int length)
    {
        var scenario = new StringScenario<TEntity>(length);
        var builder = new StringScenarioBuilder<TEntity>(scenario, this);

        Scenarios.Add(builder);
        
        return builder;
    }

    public IStringScenarioBuilder<TEntity> Empty()
    {
        var scenario = new StringScenario<TEntity>(Empty: true);
        var builder = new StringScenarioBuilder<TEntity>(scenario, this);

        Scenarios.Add(builder);
        
        return builder;
    }

    public IStringScenarioBuilder<TEntity> NotEmpty()
    {
        var scenario = new StringScenario<TEntity>(Empty: false);
        var builder = new StringScenarioBuilder<TEntity>(scenario, this);

        Scenarios.Add(builder);
        
        return builder;
    }
}

public interface IScenarioBuilder : IPropertyBuilder
{
}

public interface IScenarioBuilder<TProperty, TEntity> :
    IPropertyBuilder<TProperty, TEntity>,
    IScenarioBuilder
{
    internal IList<IScenarioBuilder<TProperty, TEntity, IPropertyBuilder<TProperty, TEntity>>> Scenarios { get; }
}

public interface IScenarioBuilder<TProperty, TEntity, out TPropertyBuilder> : 
    IScenarioBuilder<TProperty, TEntity> 
    where TPropertyBuilder : IPropertyBuilder<TProperty, TEntity>
{
    public TPropertyBuilder IsValid();
    public TPropertyBuilder IsInvalid();
}

public interface IStringScenarioBuilder<TEntity> : 
    IScenarioBuilder<string, TEntity, IStringPropertyBuilder<TEntity>>
{
}

public abstract class ScenarioBuilder<TPropertyBuilder, TProperty, TEntity, TScenario> : 
    IScenarioBuilder<TProperty, TEntity, TPropertyBuilder>
    where TPropertyBuilder : IGenericPropertyBuilder<TProperty, TEntity>
    where TScenario : IScenario<TProperty, TEntity>
{
    protected TScenario Scenario { get; }
    protected readonly TPropertyBuilder Builder;

    protected ScenarioBuilder(TScenario scenario, TPropertyBuilder builder)
    {
        Scenario = scenario;
        Builder = builder;
    }
    
    public virtual TPropertyBuilder IsValid()
    {
        var applier = new ScenarioApplier<TProperty, TEntity>(
            new(
                value => Builder.Valid(value),
                func => Builder.Valid(func)
            ),
            new(
                value => Builder.Invalid(value),
                func => Builder.Invalid(func)
            )
        );

        ApplyScenario(applier);
        
        return Builder;
    }

    public virtual TPropertyBuilder IsInvalid()
    {
        var applier = new ScenarioApplier<TProperty, TEntity>(
            new(
                value => Builder.Invalid(value),
                func => Builder.Invalid(func)
            ),
            new(
                value => Builder.Valid(value),
                func => Builder.Valid(func)
            )
        );

        ApplyScenario(applier);

        return Builder;
    }

    protected abstract void ApplyScenario(ScenarioApplier<TProperty, TEntity> applier);

    IList<IScenarioBuilder<TProperty, TEntity, IPropertyBuilder<TProperty, TEntity>>> 
        IScenarioBuilder<TProperty, TEntity>.Scenarios { get; } 
        = new List<IScenarioBuilder<TProperty, TEntity, IPropertyBuilder<TProperty, TEntity>>>();
}

public record ScenarioApplierOptions<TProperty, TEntity>(Action<TProperty> Constant,
    Action<Expression<Func<TEntity, TProperty>>> Delegate);
public record ScenarioApplier<TProperty, TEntity>(ScenarioApplierOptions<TProperty, TEntity> Success, 
    ScenarioApplierOptions<TProperty, TEntity> Failure);

public class StringScenarioBuilder<TEntity> :
    ScenarioBuilder<IStringPropertyBuilder<TEntity>, string, TEntity, StringScenario<TEntity>>,
    IStringScenarioBuilder<TEntity>
{
    public StringScenarioBuilder(StringScenario<TEntity> scenario, IStringPropertyBuilder<TEntity> builder) 
        : base(scenario, builder)
    {
    }

    protected override void ApplyScenario(ScenarioApplier<string, TEntity> applier)
    {
        ApplyLengthStart(applier);
        ApplyLengthEnd(applier);
        ApplyEmpty(applier);
    }

    private void ApplyEmpty(ScenarioApplier<string, TEntity> applier)
    {
        if (Scenario.Empty is null) return;

        var @delegate = Scenario.Empty is true ? applier.Success.Constant : applier.Failure.Constant;
        
        @delegate(Scenario.CreateShortestString(' '));
        @delegate(Scenario.CreateLongestString(' '));
    }

    private void ApplyLengthStart(ScenarioApplier<string, TEntity> applier)
    {
        var length = Scenario.ShortestLength;
        if (length <= 0) return;

        applier.Success.Constant(Scenario.CreateShortestString());
        applier.Failure.Constant(Scenario.CreateString(length - 1));
    }
    
    private void ApplyLengthEnd(ScenarioApplier<string, TEntity> applier)
    {
        var startLength = Scenario.ShortestLength;
        var endLength = Scenario.LongestLength;
        if (endLength is null) return;
        if (startLength >= endLength) return;

        applier.Success.Constant(Scenario.CreateLongestString());

        if (Scenario.LengthEnd is not int.MaxValue)
        {
            applier.Failure.Constant(Scenario.CreateString(endLength.Value + 1));
        }
    }
}

public interface IScenario<in TProperty, TEntity>
{
}

public record StringScenario<TEntity>(
    int? LengthStart = null,
    int? LengthEnd = null,
    bool? Empty = null
) : IScenario<string, TEntity>
{
    public int? LongestLength => LengthEnd ?? LengthStart;
    public int ShortestLength => LengthStart ?? 0;
    
    public string CreateShortestString(char? character = null)
    {
        return CreateString(LengthStart ?? 0, character ?? 'a');
    }

    public string CreateLongestString(char? character = null)
    {
        return CreateString(LengthEnd ?? LengthStart ?? 0, character ?? 'a');
    }

    public string CreateString(int length, char? character = null)
    {
        character ??= 'a';
        
        return new(character.Value, length);
    }
}