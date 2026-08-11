using System;
using DomainFixture.TestGenerator.Model.Properties;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DomainFixture.TestGenerator.Model.Validation;

public sealed class PropertyMutationDescriptor
{
    public PropertyDescriptor Property { get; }
    public ExpressionSyntax Value { get; }
    public ExpressionSyntax? ReconstructionFactory { get; }

    public PropertyMutationDescriptor(
        PropertyDescriptor property,
        ExpressionSyntax value,
        ExpressionSyntax? reconstructionFactory = null)
    {
        Property = property ?? throw new ArgumentNullException(nameof(property));
        Value = value ?? throw new ArgumentNullException(nameof(value));
        ReconstructionFactory = reconstructionFactory;
    }
}
