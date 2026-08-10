using System;
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

    IFixtureRecipeBuilder<TSubject> ValidateWith<TValidator>(Func<TValidator> factory)
        where TValidator : IFixtureValidator<TSubject>;

    IFixtureRecipeBuilder<TSubject> RulesFrom<TValidationRules>();
}
