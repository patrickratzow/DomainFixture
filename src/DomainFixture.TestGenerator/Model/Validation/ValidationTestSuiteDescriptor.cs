using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DomainFixture.TestGenerator.Model.Validation;

public sealed class ValidationTestSuiteDescriptor
{
    public string NamespaceName { get; }
    public string ClassName { get; }
    public string RecipeName { get; }
    public TypeSyntax SubjectType { get; }
    public ExpressionSyntax BaselineFactory { get; }
    public ExpressionSyntax ValidatorFactory { get; }
    public IReadOnlyList<GeneratedValidationCase> Cases { get; }
    public ValidationExecutionMode ExecutionMode { get; }

    public ValidationTestSuiteDescriptor(
        string namespaceName,
        string className,
        string recipeName,
        TypeSyntax subjectType,
        ExpressionSyntax baselineFactory,
        ExpressionSyntax validatorFactory,
        IEnumerable<GeneratedValidationCase> cases,
        ValidationExecutionMode executionMode = ValidationExecutionMode.Synchronous)
    {
        if (string.IsNullOrWhiteSpace(namespaceName))
            throw new ArgumentException("A namespace is required.", nameof(namespaceName));
        if (string.IsNullOrWhiteSpace(className))
            throw new ArgumentException("A class name is required.", nameof(className));
        if (string.IsNullOrWhiteSpace(recipeName))
            throw new ArgumentException("A recipe name is required.", nameof(recipeName));

        NamespaceName = namespaceName;
        ClassName = className;
        RecipeName = recipeName;
        SubjectType = subjectType ?? throw new ArgumentNullException(nameof(subjectType));
        BaselineFactory = baselineFactory ?? throw new ArgumentNullException(nameof(baselineFactory));
        ValidatorFactory = validatorFactory ?? throw new ArgumentNullException(nameof(validatorFactory));
        Cases = cases?.ToArray() ?? throw new ArgumentNullException(nameof(cases));
        ExecutionMode = executionMode;
    }
}
