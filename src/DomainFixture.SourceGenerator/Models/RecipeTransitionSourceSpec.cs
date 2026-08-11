using System;
using Microsoft.CodeAnalysis;

namespace DomainFixture.SourceGenerator.Models;

internal sealed class RecipeTransitionSourceSpec
{
    public string RecipeName { get; }
    public string TransitionName { get; }
    public Location? Location { get; }

    public RecipeTransitionSourceSpec(
        string recipeName,
        string transitionName,
        Location? location)
    {
        if (string.IsNullOrWhiteSpace(recipeName))
            throw new ArgumentException("A source recipe name is required.", nameof(recipeName));
        if (string.IsNullOrWhiteSpace(transitionName))
            throw new ArgumentException("A source transition name is required.", nameof(transitionName));

        RecipeName = recipeName;
        TransitionName = transitionName;
        Location = location;
    }
}
