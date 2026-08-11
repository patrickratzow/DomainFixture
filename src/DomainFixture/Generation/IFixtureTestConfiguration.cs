using System;
using System.Linq.Expressions;
using DomainFixture.Validation;

namespace DomainFixture.Generation;

/// <summary>
/// Declares generated validation-test recipes for <typeparamref name="TSubject"/>.
/// The source generator analyzes <see cref="Configure"/> without executing it.
/// </summary>
public interface IFixtureTestConfiguration<TSubject>
{
    void Configure(IFixtureTestBuilder<TSubject> fixture);
}

public interface IFixtureTestBuilder<TSubject>
{
    IFixtureRecipeBuilder<TSubject> Recipe(string name);
}

public interface IFixtureRecipeBuilder<TSubject>
{
    IFixtureRecipeBuilder<TSubject> Baseline(Func<TSubject> factory);

    /// <summary>
    /// Requests a valid baseline synthesized from discovered construction operations and constraints.
    /// </summary>
    IFixtureRecipeBuilder<TSubject> Synthesize();

    /// <summary>
    /// Derives this recipe's valid baseline by creating another recipe and applying one of its
    /// declared successful transitions. Transition arguments marked with <see cref="FixtureValue.Auto{T}"/>
    /// are resolved through the normal valid-value pipeline.
    /// </summary>
    IFixtureRecipeBuilder<TSubject> FromTransition(
        string recipeName,
        string transitionName);

    IFixtureRecipeBuilder<TSubject> ValidateWith<TValidator>(Func<TValidator> factory)
        where TValidator : IFixtureValidator<TSubject>;

    IFixtureRecipeBuilder<TSubject> RulesFrom<TValidationRules>();

    IFixtureRecipeBuilder<TSubject> Transition<TState>(
        string name,
        Expression<Action<TSubject>> command,
        Expression<Func<TSubject, TState>> state,
        TState expectedState);

    IFixtureRecipeBuilder<TSubject> Transition<TState>(
        string name,
        Expression<Func<TSubject, TSubject>> command,
        Expression<Func<TSubject, TState>> state,
        TState expectedState);

    IFixtureRecipeBuilder<TSubject> Transition<TResult>(
        string name,
        Expression<Func<TSubject, TResult>> command,
        Expression<Func<TResult, bool>> resultPredicate);

    IFixtureRecipeBuilder<TSubject> State<TState>(
        string name,
        Expression<Func<TSubject, TState>> path,
        TState expectedState);

    IFixtureRecipeBuilder<TSubject> RejectTransition<TException>(
        string name,
        Expression<Action<TSubject>> command)
        where TException : Exception;
}
