using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using DomainFixture.Contracts;
using DomainFixture.SourceGenerator.Diagnostics;
using DomainFixture.SourceGenerator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DomainFixture.SourceGenerator.Discovery;

internal static class FluentFixtureConfigurationProvider
{
    private const string GenerationNamespace = "DomainFixture.Generation";
    private const string ConfigurationInterfaceMetadataName = "IFixtureTestConfiguration`1";
    private const string ValidatorInterfaceMetadataName = "IFixtureValidator`1";
    private const string ValidatorNamespace = "DomainFixture.Validation";

    public static IncrementalValuesProvider<ConfigurationParseResult> Create(
        IncrementalGeneratorInitializationContext context)
    {
        return context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is ClassDeclarationSyntax { BaseList: not null },
                static (syntaxContext, cancellationToken) =>
                    ParseCandidate(syntaxContext, cancellationToken))
            .Where(static result => result is not null)
            .Select(static (result, _) => result!);
    }

    private static ConfigurationParseResult? ParseCandidate(
        GeneratorSyntaxContext context,
        CancellationToken cancellationToken)
    {
        var declaration = (ClassDeclarationSyntax)context.Node;
        if (context.SemanticModel.GetDeclaredSymbol(declaration, cancellationToken) is not INamedTypeSymbol type)
            return null;

        var configurationInterface = type.AllInterfaces.FirstOrDefault(candidate =>
            candidate.OriginalDefinition.MetadataName == ConfigurationInterfaceMetadataName &&
            candidate.OriginalDefinition.ContainingNamespace.ToDisplayString() == GenerationNamespace);
        if (configurationInterface is null || configurationInterface.TypeArguments.Length != 1)
            return null;

        var subjectType = configurationInterface.TypeArguments[0];
        var configureMethod = type.GetMembers("Configure")
            .OfType<IMethodSymbol>()
            .FirstOrDefault(method => method.Parameters.Length == 1);
        var syntaxReference = configureMethod?.DeclaringSyntaxReferences.FirstOrDefault();
        if (syntaxReference?.GetSyntax(cancellationToken) is not MethodDeclarationSyntax configureDeclaration)
        {
            return Failure(GeneratorDiagnostics.IncompleteConfiguration(
                type.Locations.FirstOrDefault(),
                type.Name));
        }

        var semanticModel = context.SemanticModel.Compilation.GetSemanticModel(
            configureDeclaration.SyntaxTree);
        var configurations = ImmutableArray.CreateBuilder<FixtureGenerationSpec>();
        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

        var recipeChains = configureDeclaration.DescendantNodes()
            .OfType<ExpressionStatementSyntax>()
            .Select(statement => statement.Expression as InvocationExpressionSyntax)
            .Where(invocation => invocation is not null &&
                EnumerateChain(invocation).Any(candidate =>
                    IsGenerationMethod(semanticModel, candidate, "Recipe")))
            .Select(invocation => invocation!)
            .Concat(configureDeclaration.ExpressionBody?.Expression is InvocationExpressionSyntax expressionBody &&
                    EnumerateChain(expressionBody).Any(candidate =>
                        IsGenerationMethod(semanticModel, candidate, "Recipe"))
                ? new[] { expressionBody }
                : System.Array.Empty<InvocationExpressionSyntax>())
            .ToArray();
        if (recipeChains.Length == 0)
        {
            diagnostics.Add(GeneratorDiagnostics.IncompleteConfiguration(
                configureDeclaration.GetLocation(),
                type.Name));
        }

        foreach (var outerInvocation in recipeChains)
        {
            var parsed = ParseRecipeChain(
                semanticModel,
                type,
                subjectType,
                outerInvocation,
                diagnostics);
            if (parsed is not null)
                configurations.Add(parsed);
        }

        foreach (var duplicate in configurations
                     .GroupBy(configuration => configuration.RecipeName)
                     .Where(group => group.Count() > 1))
        {
            diagnostics.Add(GeneratorDiagnostics.RecipeDuplicated(
                type.Locations.FirstOrDefault(),
                type.Name,
                duplicate.Key));
        }

        return new ConfigurationParseResult(configurations.ToImmutable(), diagnostics.ToImmutable());
    }

    private static FixtureGenerationSpec? ParseRecipeChain(
        SemanticModel semanticModel,
        INamedTypeSymbol configurationType,
        ITypeSymbol subjectType,
        InvocationExpressionSyntax outerInvocation,
        ImmutableArray<Diagnostic>.Builder diagnostics)
    {
        string? recipeName = null;
        IMethodSymbol? baselineFactory = null;
        var synthesizeRequested = false;
        RecipeTransitionSourceSpec? transitionSource = null;
        IMethodSymbol? validatorFactory = null;
        INamedTypeSymbol? validationRulesType = null;
        var transitions = ImmutableArray.CreateBuilder<DomainTransitionSpec>();
        var stateExpectations = ImmutableArray.CreateBuilder<DomainStateExpectationSpec>();
        var inferredValues = ImmutableArray.CreateBuilder<InferredValueSpec>();

        foreach (var invocation in EnumerateChain(outerInvocation))
        {
            if (semanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method ||
                method.ContainingNamespace.ToDisplayString() != GenerationNamespace)
            {
                continue;
            }

            switch (method.Name)
            {
                case "Recipe" when invocation.ArgumentList.Arguments.Count == 1:
                    var recipeValue = semanticModel.GetConstantValue(
                        invocation.ArgumentList.Arguments[0].Expression);
                    recipeName = recipeValue.HasValue ? recipeValue.Value as string : null;
                    break;

                case "Baseline" when invocation.ArgumentList.Arguments.Count == 1:
                    baselineFactory = ResolveMethodGroup(
                        semanticModel,
                        invocation.ArgumentList.Arguments[0].Expression);
                    break;

                case "Synthesize" when invocation.ArgumentList.Arguments.Count == 0:
                    synthesizeRequested = true;
                    break;

                case "FromTransition" when invocation.ArgumentList.Arguments.Count == 2:
                    var sourceRecipe = semanticModel.GetConstantValue(
                        invocation.ArgumentList.Arguments[0].Expression);
                    var sourceTransition = semanticModel.GetConstantValue(
                        invocation.ArgumentList.Arguments[1].Expression);
                    if (sourceRecipe.HasValue &&
                        sourceRecipe.Value is string sourceRecipeName &&
                        !string.IsNullOrWhiteSpace(sourceRecipeName) &&
                        sourceTransition.HasValue &&
                        sourceTransition.Value is string sourceTransitionName &&
                        !string.IsNullOrWhiteSpace(sourceTransitionName))
                    {
                        transitionSource = new RecipeTransitionSourceSpec(
                            sourceRecipeName,
                            sourceTransitionName,
                            invocation.GetLocation());
                    }
                    break;

                case "ValidateWith" when invocation.ArgumentList.Arguments.Count == 1:
                    validatorFactory = ResolveMethodGroup(
                        semanticModel,
                        invocation.ArgumentList.Arguments[0].Expression);
                    break;

                case "RulesFrom" when method.TypeArguments.FirstOrDefault() is INamedTypeSymbol rulesType:
                    validationRulesType = rulesType;
                    break;

                case "Transition" when invocation.ArgumentList.Arguments.Count is 3 or 4:
                    var transition = ParseTransition(
                        semanticModel,
                        subjectType,
                        invocation,
                        isRejection: false,
                        diagnostics,
                        inferredValues);
                    if (transition is not null)
                        transitions.Add(transition);
                    break;

                case "RejectTransition" when invocation.ArgumentList.Arguments.Count == 2:
                    var rejection = ParseTransition(
                        semanticModel,
                        subjectType,
                        invocation,
                        isRejection: true,
                        diagnostics,
                        inferredValues);
                    if (rejection is not null)
                        transitions.Add(rejection);
                    break;

                case "State" when invocation.ArgumentList.Arguments.Count == 3:
                    var state = ParseStateExpectation(
                        semanticModel,
                        subjectType,
                        invocation,
                        diagnostics);
                    if (state is not null)
                        stateExpectations.Add(state);
                    break;
            }
        }

        foreach (var duplicate in transitions
                     .GroupBy(transition => transition.Name, System.StringComparer.Ordinal)
                     .Where(group => group.Count() > 1))
        {
            diagnostics.Add(GeneratorDiagnostics.TransitionDuplicated(
                duplicate.First().Location ?? outerInvocation.GetLocation(),
                recipeName ?? configurationType.Name,
                duplicate.Key));
        }

        foreach (var duplicate in stateExpectations
                     .GroupBy(state => state.Name, System.StringComparer.Ordinal)
                     .Where(group => group.Count() > 1))
        {
            diagnostics.Add(GeneratorDiagnostics.TransitionDuplicated(
                duplicate.First().Location ?? outerInvocation.GetLocation(),
                recipeName ?? configurationType.Name,
                duplicate.Key));
        }

        var baselineDeclarationCount =
            (baselineFactory is null ? 0 : 1) +
            (synthesizeRequested ? 1 : 0) +
            (transitionSource is null ? 0 : 1);
        if (string.IsNullOrWhiteSpace(recipeName) || baselineDeclarationCount > 1)
        {
            diagnostics.Add(GeneratorDiagnostics.IncompleteConfiguration(
                outerInvocation.GetLocation(),
                configurationType.Name));
            return null;
        }

        if (baselineFactory is not null && !IsCallableFactory(baselineFactory))
        {
            diagnostics.Add(GeneratorDiagnostics.InaccessibleFactory(
                baselineFactory.Locations.FirstOrDefault() ?? outerInvocation.GetLocation(),
                baselineFactory.ToDisplayString()));
            return null;
        }

        if (validatorFactory is not null && !IsCallableFactory(validatorFactory))
        {
            diagnostics.Add(GeneratorDiagnostics.InaccessibleFactory(
                validatorFactory.Locations.FirstOrDefault() ?? outerInvocation.GetLocation(),
                validatorFactory.ToDisplayString()));
            return null;
        }

        if (baselineFactory is not null &&
            !SymbolEqualityComparer.Default.Equals(baselineFactory.ReturnType, subjectType))
        {
            diagnostics.Add(GeneratorDiagnostics.IncompleteConfiguration(
                outerInvocation.GetLocation(),
                configurationType.Name));
            return null;
        }

        if (validatorFactory is not null &&
            !ImplementsFixtureValidator(validatorFactory.ReturnType, subjectType))
        {
            diagnostics.Add(GeneratorDiagnostics.IncompatibleValidator(
                validatorFactory.Locations.FirstOrDefault() ?? outerInvocation.GetLocation(),
                configurationType.Name,
                subjectType.ToDisplayString()));
            return null;
        }

        var namespaceName = configurationType.ContainingNamespace.IsGlobalNamespace
            ? "DomainFixture.Generated"
            : configurationType.ContainingNamespace.ToDisplayString();

        var subjectProperties = DiscoverProperties(subjectType, semanticModel.Compilation.Assembly);
        var reconstructionConstructors = DiscoverReconstructionConstructors(
            subjectType,
            semanticModel.Compilation.Assembly);
        var constructionOperations = ConstructionOperationDiscovery.Discover(
            subjectType,
            semanticModel.Compilation.Assembly,
            diagnostics,
            outerInvocation.GetLocation());
        inferredValues.AddRange(
            ConventionalValueExpressionDiscovery.DiscoverReferencedValues(
                subjectType,
                semanticModel.Compilation.Assembly));
        var distinctInferredValues = inferredValues
            .GroupBy(value => value.TypeName, System.StringComparer.Ordinal)
            .Select(group => group.First())
            .OrderBy(value => value.TypeName, System.StringComparer.Ordinal)
            .ToImmutableArray();
        return new FixtureGenerationSpec(
            configurationType.Name,
            namespaceName,
            recipeName!,
            subjectType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            subjectType.Name,
            baselineFactory is null ? string.Empty : CreateFactoryExpression(baselineFactory),
            validatorFactory is null ? null : CreateFactoryExpression(validatorFactory),
            validationRulesType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            subjectProperties,
            subjectType is INamedTypeSymbol { IsRecord: true },
            reconstructionConstructors,
            CanUseDerivedReconstruction(subjectType, semanticModel.Compilation.Assembly),
            HasValueEqualitySemantics(subjectType),
            DiscoverEquivalentCopyExpression(
                subjectType,
                semanticModel.Compilation.Assembly,
                reconstructionConstructors),
            constructionOperations,
            outerInvocation.GetLocation(),
            CanExposePublicFactory(subjectType),
            transitions.ToImmutable(),
            identityMemberPath: null,
            usesSynthesizedBaseline: synthesizeRequested,
            stateExpectations: stateExpectations.ToImmutable(),
            transitionSource: transitionSource,
            inferredValues: distinctInferredValues);
    }

    private static DomainTransitionSpec? ParseTransition(
        SemanticModel semanticModel,
        ITypeSymbol subjectType,
        InvocationExpressionSyntax invocation,
        bool isRejection,
        ImmutableArray<Diagnostic>.Builder diagnostics,
        ImmutableArray<InferredValueSpec>.Builder inferredValues)
    {
        var nameExpression = invocation.ArgumentList.Arguments[0].Expression;
        var nameValue = semanticModel.GetConstantValue(nameExpression);
        var transitionName = nameValue.HasValue ? nameValue.Value as string : null;
        var diagnosticName = string.IsNullOrWhiteSpace(transitionName)
            ? nameExpression.ToString()
            : transitionName!;
        if (string.IsNullOrWhiteSpace(transitionName))
        {
            diagnostics.Add(GeneratorDiagnostics.TransitionStateInvalid(
                nameExpression.GetLocation(),
                diagnosticName,
                "the transition name must be a non-empty constant string"));
            return null;
        }

        var commandExpression = invocation.ArgumentList.Arguments[1].Expression;
        var commandLambda = UnwrapLambda(commandExpression);
        if (!TryResolveDirectSubjectInvocation(
                semanticModel,
                commandLambda,
                subjectType,
                out var commandMethod,
                out var commandInvocation) ||
            commandMethod is null ||
            commandInvocation is null ||
            commandMethod.MethodKind != MethodKind.Ordinary ||
            commandMethod.IsStatic ||
            commandMethod.IsGenericMethod ||
            commandMethod.Parameters.Length != commandInvocation.ArgumentList.Arguments.Count ||
            !IsAccessibleFromGeneratedCode(
                commandMethod,
                semanticModel.Compilation.Assembly))
        {
            diagnostics.Add(GeneratorDiagnostics.TransitionOperationInvalid(
                commandExpression.GetLocation(),
                transitionName!));
            return null;
        }

        var subjectTypeName = subjectType.ToDisplayString(
            SymbolDisplayFormat.FullyQualifiedFormat);
        var declaringTypeName = commandMethod.ContainingType.ToDisplayString(
            SymbolDisplayFormat.FullyQualifiedFormat);
        var commandArguments = ImmutableArray.CreateBuilder<DomainCommandArgumentSpec>();
        var operationParameters = ImmutableArray.CreateBuilder<DomainOperationParameterContract>();
        for (var index = 0; index < commandMethod.Parameters.Length; index++)
        {
            var parameter = commandMethod.Parameters[index];
            inferredValues.AddRange(
                ConventionalValueExpressionDiscovery.DiscoverValue(
                    parameter.Type,
                    semanticModel.Compilation.Assembly));
            var argumentExpression = commandInvocation.ArgumentList.Arguments[index].Expression;
            var isAuto = IsFixtureValueAuto(semanticModel, argumentExpression);
            var emittedArgument = isAuto
                ? "default!"
                : CreateExpectedStateExpression(semanticModel, argumentExpression);
            if (emittedArgument is null)
            {
                diagnostics.Add(GeneratorDiagnostics.TransitionOperationInvalid(
                    argumentExpression.GetLocation(),
                    transitionName!));
                return null;
            }

            var parameterTypeName = parameter.Type.ToDisplayString(
                SymbolDisplayFormat.FullyQualifiedFormat);
            commandArguments.Add(new DomainCommandArgumentSpec(
                index,
                parameter.Name,
                parameterTypeName,
                emittedArgument,
                isAuto
                    ? DomainCommandArgumentSource.AutoFixtureValue
                    : DomainCommandArgumentSource.ExplicitExpression));
            operationParameters.Add(new DomainOperationParameterContract(
                parameter.Name,
                parameterTypeName,
                parameter.Name));
        }

        var immutableCommand = SymbolEqualityComparer.Default.Equals(
            commandMethod.ReturnType,
            subjectType);
        var resultCommand = !isRejection && invocation.ArgumentList.Arguments.Count == 3;
        if (isRejection && !commandMethod.ReturnsVoid ||
            !isRejection && !resultCommand && !commandMethod.ReturnsVoid && !immutableCommand ||
            resultCommand && commandMethod.ReturnsVoid)
        {
            diagnostics.Add(GeneratorDiagnostics.TransitionOperationInvalid(
                commandExpression.GetLocation(),
                transitionName!));
            return null;
        }

        var operationParameterArray = operationParameters.ToImmutable();
        var operation = new DomainOperationContract(
            DomainOperationContract.CreateOperationId(
                DomainOperationKinds.InstanceCommand,
                declaringTypeName,
                commandMethod.Name,
                operationParameterArray),
            DomainOperationKinds.InstanceCommand,
            declaringTypeName,
            subjectTypeName,
            commandMethod.Name,
            commandMethod.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            operationParameterArray);

        if (isRejection)
        {
            var invokedBuilderMethod = semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
            var exceptionType = invokedBuilderMethod?.TypeArguments.FirstOrDefault();
            if (exceptionType is null)
            {
                diagnostics.Add(GeneratorDiagnostics.TransitionStateInvalid(
                    invocation.GetLocation(),
                    transitionName!,
                    "the rejection exception type could not be resolved"));
                return null;
            }

            return new DomainTransitionSpec(
                transitionName!,
                operation,
                commandArguments.ToImmutable(),
                DomainTransitionExecutionKind.MutatingCommand,
                stateName: null,
                stateMemberPath: null,
                expectedStateExpression: null,
                resultTypeName: null,
                resultPredicateExpression: null,
                exceptionType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                invocation.GetLocation());
        }

        if (resultCommand)
        {
            var predicateExpression = invocation.ArgumentList.Arguments[2].Expression;
            if (UnwrapLambda(predicateExpression) is null)
            {
                diagnostics.Add(GeneratorDiagnostics.TransitionStateInvalid(
                    predicateExpression.GetLocation(),
                    transitionName!,
                    "the result predicate must be a synchronous lambda expression"));
                return null;
            }

            return new DomainTransitionSpec(
                transitionName!,
                operation,
                commandArguments.ToImmutable(),
                DomainTransitionExecutionKind.ResultCommand,
                stateName: null,
                stateMemberPath: null,
                expectedStateExpression: null,
                commandMethod.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                predicateExpression.NormalizeWhitespace().ToFullString(),
                rejectionExceptionTypeName: null,
                invocation.GetLocation());
        }

        var stateExpression = invocation.ArgumentList.Arguments[2].Expression;
        var stateLambda = UnwrapLambda(stateExpression);
        if (!TryResolveSubjectMemberPath(
                semanticModel,
                stateLambda,
                subjectType,
                out var stateMemberName))
        {
            diagnostics.Add(GeneratorDiagnostics.TransitionStateInvalid(
                stateExpression.GetLocation(),
                transitionName!,
                "the state selector must read one accessible instance property or field directly from the subject"));
            return null;
        }

        var expectedExpression = invocation.ArgumentList.Arguments[3].Expression;
        var emittedExpectedState = CreateExpectedStateExpression(
            semanticModel,
            expectedExpression);
        if (emittedExpectedState is null)
        {
            diagnostics.Add(GeneratorDiagnostics.TransitionStateInvalid(
                expectedExpression.GetLocation(),
                transitionName!,
                "the expected state must be a literal, enum member, or accessible static field or property"));
            return null;
        }

        return new DomainTransitionSpec(
            transitionName!,
            operation,
            commandArguments.ToImmutable(),
            immutableCommand
                ? DomainTransitionExecutionKind.ImmutableCommand
                : DomainTransitionExecutionKind.MutatingCommand,
            stateName: transitionName,
            stateMemberPath: stateMemberName,
            expectedStateExpression: emittedExpectedState,
            resultTypeName: null,
            resultPredicateExpression: null,
            rejectionExceptionTypeName: null,
            invocation.GetLocation());
    }

    private static DomainStateExpectationSpec? ParseStateExpectation(
        SemanticModel semanticModel,
        ITypeSymbol subjectType,
        InvocationExpressionSyntax invocation,
        ImmutableArray<Diagnostic>.Builder diagnostics)
    {
        var nameExpression = invocation.ArgumentList.Arguments[0].Expression;
        var nameValue = semanticModel.GetConstantValue(nameExpression);
        var name = nameValue.HasValue ? nameValue.Value as string : null;
        if (string.IsNullOrWhiteSpace(name))
        {
            diagnostics.Add(GeneratorDiagnostics.TransitionStateInvalid(
                nameExpression.GetLocation(),
                nameExpression.ToString(),
                "the state name must be a non-empty constant string"));
            return null;
        }

        var pathExpression = invocation.ArgumentList.Arguments[1].Expression;
        if (!TryResolveSubjectMemberPath(
                semanticModel,
                UnwrapLambda(pathExpression),
                subjectType,
                out var memberPath))
        {
            diagnostics.Add(GeneratorDiagnostics.TransitionStateInvalid(
                pathExpression.GetLocation(),
                name!,
                "the state path must be a readable property or field path from the subject"));
            return null;
        }

        var expectedExpression = invocation.ArgumentList.Arguments[2].Expression;
        var emittedExpected = CreateExpectedStateExpression(semanticModel, expectedExpression);
        if (emittedExpected is null)
        {
            diagnostics.Add(GeneratorDiagnostics.TransitionStateInvalid(
                expectedExpression.GetLocation(),
                name!,
                "the expected state must be a literal, enum member, or accessible static member"));
            return null;
        }

        return new DomainStateExpectationSpec(
            name!,
            memberPath!,
            emittedExpected,
            invocation.GetLocation());
    }

    private static LambdaExpressionSyntax? UnwrapLambda(ExpressionSyntax expression)
    {
        while (expression is ParenthesizedExpressionSyntax parenthesized)
            expression = parenthesized.Expression;

        return expression as LambdaExpressionSyntax;
    }

    private static bool TryResolveDirectSubjectInvocation(
        SemanticModel semanticModel,
        LambdaExpressionSyntax? lambda,
        ITypeSymbol subjectType,
        out IMethodSymbol? method,
        out InvocationExpressionSyntax? invocation)
    {
        method = null;
        invocation = null;
        if (!TryGetSingleLambdaParameter(semanticModel, lambda, subjectType, out var parameter) ||
            lambda!.Body is not InvocationExpressionSyntax resolvedInvocation ||
            resolvedInvocation.Expression is not MemberAccessExpressionSyntax memberAccess ||
            !SymbolEqualityComparer.Default.Equals(
                semanticModel.GetSymbolInfo(memberAccess.Expression).Symbol,
                parameter))
        {
            return false;
        }

        invocation = resolvedInvocation;
        method = semanticModel.GetSymbolInfo(resolvedInvocation).Symbol as IMethodSymbol;
        return method is not null;
    }

    private static bool TryResolveSubjectMemberPath(
        SemanticModel semanticModel,
        LambdaExpressionSyntax? lambda,
        ITypeSymbol subjectType,
        out string? memberPath)
    {
        memberPath = null;
        if (!TryGetSingleLambdaParameter(semanticModel, lambda, subjectType, out var parameter) ||
            lambda!.Body is not MemberAccessExpressionSyntax memberAccess)
        {
            return false;
        }

        var segments = new List<string>();
        var currentAssembly = semanticModel.Compilation.Assembly;
        ExpressionSyntax current = memberAccess;
        while (current is MemberAccessExpressionSyntax currentAccess)
        {
            var member = semanticModel.GetSymbolInfo(currentAccess).Symbol;
            switch (member)
            {
                case IPropertySymbol property when
                    !property.IsStatic && property.Parameters.Length == 0 &&
                    IsAccessibleFromGeneratedCode(property.GetMethod, currentAssembly):
                    segments.Insert(0, EscapeIdentifier(property.Name));
                    break;
                case IFieldSymbol field when
                    !field.IsStatic && IsAccessibleFromGeneratedCode(field, currentAssembly):
                    segments.Insert(0, EscapeIdentifier(field.Name));
                    break;
                default:
                    return false;
            }

            current = currentAccess.Expression;
        }

        if (!SymbolEqualityComparer.Default.Equals(
                semanticModel.GetSymbolInfo(current).Symbol,
                parameter))
            return false;

        memberPath = string.Join(".", segments);
        return segments.Count > 0;
    }

    private static bool IsFixtureValueAuto(
        SemanticModel semanticModel,
        ExpressionSyntax expression)
    {
        return expression is InvocationExpressionSyntax invocation &&
               semanticModel.GetSymbolInfo(invocation).Symbol is IMethodSymbol
               {
                   Name: "Auto",
                   ContainingType.Name: "FixtureValue"
               } method &&
               method.ContainingNamespace.ToDisplayString() == GenerationNamespace;
    }

    private static bool TryGetSingleLambdaParameter(
        SemanticModel semanticModel,
        LambdaExpressionSyntax? lambda,
        ITypeSymbol subjectType,
        out IParameterSymbol? parameter)
    {
        parameter = null;
        ParameterSyntax? parameterSyntax = lambda switch
        {
            SimpleLambdaExpressionSyntax simple => simple.Parameter,
            ParenthesizedLambdaExpressionSyntax { ParameterList.Parameters.Count: 1 } parenthesized =>
                parenthesized.ParameterList.Parameters[0],
            _ => null
        };
        if (parameterSyntax is null)
            return false;

        parameter = semanticModel.GetDeclaredSymbol(parameterSyntax) as IParameterSymbol;
        return parameter is not null &&
               SymbolEqualityComparer.Default.Equals(parameter.Type, subjectType);
    }

    private static string? CreateExpectedStateExpression(
        SemanticModel semanticModel,
        ExpressionSyntax expression)
    {
        while (expression is ParenthesizedExpressionSyntax parenthesized)
            expression = parenthesized.Expression;

        if (expression is LiteralExpressionSyntax ||
            expression is PrefixUnaryExpressionSyntax { Operand: LiteralExpressionSyntax })
        {
            return expression.NormalizeWhitespace().ToFullString();
        }

        var symbol = semanticModel.GetSymbolInfo(expression).Symbol;
        var currentAssembly = semanticModel.Compilation.Assembly;
        switch (symbol)
        {
            case IFieldSymbol field when
                field.IsStatic && IsAccessibleFromGeneratedCode(field, currentAssembly):
                return $"{field.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{EscapeIdentifier(field.Name)}";
            case IPropertySymbol property when
                property.IsStatic && IsAccessibleFromGeneratedCode(property.GetMethod, currentAssembly):
                return $"{property.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{EscapeIdentifier(property.Name)}";
            default:
                return null;
        }
    }

    private static string EscapeIdentifier(string identifier)
    {
        return SyntaxFacts.GetKeywordKind(identifier) != SyntaxKind.None ||
               SyntaxFacts.GetContextualKeywordKind(identifier) != SyntaxKind.None
            ? "@" + identifier
            : identifier;
    }

    internal static ImmutableArray<SubjectPropertySpec> DiscoverProperties(
        ITypeSymbol subjectType,
        IAssemblySymbol currentAssembly)
    {
        var properties = ImmutableArray.CreateBuilder<SubjectPropertySpec>();
        var seenNames = new HashSet<string>();

        for (var current = subjectType as INamedTypeSymbol; current is not null; current = current.BaseType)
        {
            foreach (var property in current.GetMembers().OfType<IPropertySymbol>())
            {
                if (property.IsStatic || property.Parameters.Length != 0 || !seenNames.Add(property.Name))
                    continue;

                var setterAccessible = IsAccessibleFromGeneratedCode(
                    property.SetMethod,
                    currentAssembly);

                properties.Add(new SubjectPropertySpec(
                    property.Name,
                    property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    property.Type.SpecialType == SpecialType.System_String,
                    property.NullableAnnotation == NullableAnnotation.NotAnnotated,
                    setterAccessible && property.SetMethod?.IsInitOnly != true,
                    setterAccessible,
                    property.SetMethod is not null,
                    IsSetterAccessibleFromDerivedType(property.SetMethod, currentAssembly),
                    IsAccessibleFromGeneratedCode(property.GetMethod, currentAssembly)));
            }
        }

        return properties.ToImmutable();
    }

    internal static ImmutableArray<SubjectConstructorSpec> DiscoverReconstructionConstructors(
        ITypeSymbol subjectType,
        IAssemblySymbol currentAssembly)
    {
        if (subjectType is not INamedTypeSymbol namedType)
            return ImmutableArray<SubjectConstructorSpec>.Empty;

        var readableProperties = EnumerateProperties(namedType)
            .Where(property =>
                property.GetMethod is not null &&
                IsAccessibleFromGeneratedCode(property.GetMethod, currentAssembly))
            .ToArray();
        var constructors = ImmutableArray.CreateBuilder<SubjectConstructorSpec>();

        foreach (var constructor in namedType.InstanceConstructors.Where(constructor =>
                     constructor.Parameters.Length > 0 &&
                     IsAccessibleFromGeneratedCode(constructor, currentAssembly)))
        {
            var parameters = ImmutableArray.CreateBuilder<ConstructorParameterSpec>();
            var matchedProperties = new HashSet<string>();
            foreach (var parameter in constructor.Parameters)
            {
                var property = readableProperties.FirstOrDefault(candidate =>
                    string.Equals(candidate.Name, parameter.Name, System.StringComparison.OrdinalIgnoreCase) &&
                    SymbolEqualityComparer.Default.Equals(candidate.Type, parameter.Type));
                if (property is null || !matchedProperties.Add(property.Name))
                {
                    parameters.Clear();
                    break;
                }

                parameters.Add(new ConstructorParameterSpec(property.Name));
            }

            if (parameters.Count == constructor.Parameters.Length &&
                matchedProperties.Count == readableProperties.Length)
            {
                constructors.Add(new SubjectConstructorSpec(parameters.ToImmutable()));
            }
        }

        return constructors.ToImmutable();
    }

    private static IEnumerable<IPropertySymbol> EnumerateProperties(INamedTypeSymbol subjectType)
    {
        var seenNames = new HashSet<string>();
        for (var current = subjectType; current is not null; current = current.BaseType)
        {
            foreach (var property in current.GetMembers().OfType<IPropertySymbol>())
            {
                if (!property.IsStatic &&
                    property.Parameters.Length == 0 &&
                    seenNames.Add(property.Name))
                {
                    yield return property;
                }
            }
        }
    }

    internal static bool CanUseDerivedReconstruction(
        ITypeSymbol subjectType,
        IAssemblySymbol currentAssembly)
    {
        if (subjectType is not INamedTypeSymbol namedType ||
            namedType.TypeKind != TypeKind.Class ||
            namedType.IsSealed ||
            !namedType.InstanceConstructors.Any(constructor =>
                constructor.Parameters.Length == 0 &&
                IsConstructorAccessibleFromDerivedType(constructor, currentAssembly)))
        {
            return false;
        }

        var properties = DiscoverProperties(subjectType, currentAssembly);
        return properties.All(property =>
                property.HasSetter &&
                property.CanSetFromDerivedType &&
                property.CanReadFromGeneratedCode);
    }

    internal static bool HasValueEqualitySemantics(ITypeSymbol subjectType)
    {
        if (subjectType is not INamedTypeSymbol namedType)
            return false;
        if (namedType.IsRecord)
            return true;

        var overridesEquals = false;
        var overridesGetHashCode = false;
        for (var current = namedType; current is not null; current = current.BaseType)
        {
            overridesEquals |= current.GetMembers("Equals")
                .OfType<IMethodSymbol>()
                .Any(method =>
                    method.IsOverride &&
                    method.Parameters.Length == 1 &&
                    method.Parameters[0].Type.SpecialType == SpecialType.System_Object);
            overridesGetHashCode |= current.GetMembers("GetHashCode")
                .OfType<IMethodSymbol>()
                .Any(method => method.IsOverride && method.Parameters.Length == 0);
        }

        return overridesEquals && overridesGetHashCode;
    }

    internal static bool CanExposePublicFactory(ITypeSymbol type)
    {
        switch (type)
        {
            case IArrayTypeSymbol array:
                return CanExposePublicFactory(array.ElementType);
            case IPointerTypeSymbol pointer:
                return CanExposePublicFactory(pointer.PointedAtType);
            case ITypeParameterSymbol:
                return false;
            case INamedTypeSymbol named:
                if (named.SpecialType != SpecialType.None)
                    return true;
                if (named.DeclaredAccessibility != Accessibility.Public ||
                    named.ContainingType is not null && !CanExposePublicFactory(named.ContainingType))
                {
                    return false;
                }

                return named.TypeArguments.All(CanExposePublicFactory);
            default:
                return false;
        }
    }

    internal static string? DiscoverEquivalentCopyExpression(
        ITypeSymbol subjectType,
        IAssemblySymbol currentAssembly,
        ImmutableArray<SubjectConstructorSpec> constructors)
    {
        if (subjectType is not INamedTypeSymbol namedType)
            return null;
        if (namedType.IsRecord)
            return "subject with { }";

        var subjectTypeName = subjectType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var constructor = constructors.FirstOrDefault();
        if (constructor is not null)
        {
            return $"new {subjectTypeName}({string.Join(", ", constructor.Parameters.Select(parameter => $"subject.{parameter.PropertyName}"))})";
        }

        var readableProperties = EnumerateProperties(namedType)
            .Where(property =>
                property.GetMethod is not null &&
                IsAccessibleFromGeneratedCode(property.GetMethod, currentAssembly))
            .ToArray();
        for (var current = namedType; current is not null; current = current.BaseType)
        {
            foreach (var method in current.GetMembers().OfType<IMethodSymbol>().Where(method =>
                         method.IsStatic &&
                         method.MethodKind == MethodKind.Ordinary &&
                         method.Name is "From" or "Create" or "Of" &&
                         method.Parameters.Length == readableProperties.Length &&
                         method.Parameters.Length > 0 &&
                         SymbolEqualityComparer.Default.Equals(method.ReturnType, subjectType) &&
                         IsAccessibleFromGeneratedCode(method, currentAssembly)))
            {
                var mappedProperties = new List<IPropertySymbol>();
                foreach (var parameter in method.Parameters)
                {
                    var property = readableProperties.FirstOrDefault(candidate =>
                        !mappedProperties.Contains(candidate, SymbolEqualityComparer.Default) &&
                        SymbolEqualityComparer.Default.Equals(candidate.Type, parameter.Type) &&
                        (readableProperties.Length == 1 ||
                         string.Equals(candidate.Name, parameter.Name, System.StringComparison.OrdinalIgnoreCase)));
                    if (property is null)
                    {
                        mappedProperties.Clear();
                        break;
                    }

                    mappedProperties.Add(property);
                }

                if (mappedProperties.Count != method.Parameters.Length)
                    continue;

                var methodType = method.ContainingType.ToDisplayString(
                    SymbolDisplayFormat.FullyQualifiedFormat);
                return $"{methodType}.{method.Name}({string.Join(", ", mappedProperties.Select(property => $"subject.{property.Name}"))})";
            }
        }

        return null;
    }

    private static bool IsAccessibleFromGeneratedCode(
        IMethodSymbol? method,
        IAssemblySymbol currentAssembly)
    {
        if (method is null)
            return false;

        var sameAssembly = SymbolEqualityComparer.Default.Equals(
            method.ContainingAssembly,
            currentAssembly);
        return method.DeclaredAccessibility == Accessibility.Public ||
               sameAssembly && method.DeclaredAccessibility is Accessibility.Internal or
                   Accessibility.ProtectedOrInternal;
    }

    private static bool IsAccessibleFromGeneratedCode(
        ISymbol symbol,
        IAssemblySymbol currentAssembly)
    {
        var sameAssembly = SymbolEqualityComparer.Default.Equals(
            symbol.ContainingAssembly,
            currentAssembly);
        return symbol.DeclaredAccessibility == Accessibility.Public ||
               sameAssembly && symbol.DeclaredAccessibility is Accessibility.Internal or
                   Accessibility.ProtectedOrInternal;
    }

    private static bool IsSetterAccessibleFromDerivedType(
        IMethodSymbol? method,
        IAssemblySymbol currentAssembly)
    {
        if (method is null)
            return false;

        var sameAssembly = SymbolEqualityComparer.Default.Equals(
            method.ContainingAssembly,
            currentAssembly);
        return method.DeclaredAccessibility is Accessibility.Public or
                   Accessibility.Protected or
                   Accessibility.ProtectedOrInternal ||
               sameAssembly && method.DeclaredAccessibility is Accessibility.Internal or
                   Accessibility.ProtectedAndInternal;
    }

    private static bool IsConstructorAccessibleFromDerivedType(
        IMethodSymbol constructor,
        IAssemblySymbol currentAssembly)
    {
        var sameAssembly = SymbolEqualityComparer.Default.Equals(
            constructor.ContainingAssembly,
            currentAssembly);
        return constructor.DeclaredAccessibility is Accessibility.Public or
                   Accessibility.Protected or
                   Accessibility.ProtectedOrInternal ||
               sameAssembly && constructor.DeclaredAccessibility is Accessibility.Internal or
                   Accessibility.ProtectedAndInternal;
    }

    private static IEnumerable<InvocationExpressionSyntax> EnumerateChain(
        InvocationExpressionSyntax outerInvocation)
    {
        var current = outerInvocation;
        while (true)
        {
            yield return current;

            if (current.Expression is not MemberAccessExpressionSyntax memberAccess ||
                memberAccess.Expression is not InvocationExpressionSyntax previous)
            {
                yield break;
            }

            current = previous;
        }
    }

    private static bool IsGenerationMethod(
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocation,
        string methodName)
    {
        return semanticModel.GetSymbolInfo(invocation).Symbol is IMethodSymbol method &&
               method.Name == methodName &&
               method.ContainingNamespace.ToDisplayString() == GenerationNamespace;
    }

    private static IMethodSymbol? ResolveMethodGroup(
        SemanticModel semanticModel,
        ExpressionSyntax expression)
    {
        var symbolInfo = semanticModel.GetSymbolInfo(expression);
        return symbolInfo.Symbol as IMethodSymbol ??
               symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault();
    }

    private static bool IsCallableFactory(IMethodSymbol method)
    {
        return method.IsStatic &&
               method.Parameters.Length == 0 &&
               method.DeclaredAccessibility is Accessibility.Public or
                   Accessibility.Internal or
                   Accessibility.ProtectedOrInternal;
    }

    private static bool ImplementsFixtureValidator(ITypeSymbol validatorType, ITypeSymbol subjectType)
    {
        var candidates = validatorType is INamedTypeSymbol named
            ? named.AllInterfaces.Add(named)
            : ImmutableArray<INamedTypeSymbol>.Empty;

        return candidates.Any(candidate =>
            candidate.OriginalDefinition.MetadataName == ValidatorInterfaceMetadataName &&
            candidate.OriginalDefinition.ContainingNamespace.ToDisplayString() == ValidatorNamespace &&
            candidate.TypeArguments.Length == 1 &&
            SymbolEqualityComparer.Default.Equals(candidate.TypeArguments[0], subjectType));
    }

    private static string CreateFactoryExpression(IMethodSymbol factory)
    {
        var containingType = factory.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return $"{containingType}.{factory.Name}()";
    }

    private static ConfigurationParseResult Failure(Diagnostic diagnostic)
    {
        return new ConfigurationParseResult(
            ImmutableArray<FixtureGenerationSpec>.Empty,
            ImmutableArray.Create(diagnostic));
    }
}
