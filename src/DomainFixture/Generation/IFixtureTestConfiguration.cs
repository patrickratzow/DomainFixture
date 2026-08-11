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
