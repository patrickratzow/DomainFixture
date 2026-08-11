using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using DomainFixture.Contracts;
using DomainFixture.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace DomainFixture.SourceGenerator.Extraction;

internal static class DomainOperationManifestProvider
{
    private const string OperationAttributeMetadataName =
        "DomainFixture.Generation.Metadata.DomainOperationManifestAttribute";
    private const string OutcomeAttributeMetadataName =
        "DomainFixture.Generation.Metadata.DomainOperationOutcomeManifestAttribute";

    public static IncrementalValueProvider<DomainOperationManifestExtraction> Create(
        IncrementalGeneratorInitializationContext context) =>
        context.CompilationProvider.Select(static (compilation, _) => Extract(compilation));

    internal static DomainOperationManifestExtraction Extract(Compilation compilation)
    {
        var operations = ImmutableArray.CreateBuilder<DiscoveredDomainOperation>();
        var outcomes = ImmutableArray.CreateBuilder<DiscoveredDomainOperationOutcome>();
        var failures = ImmutableArray.CreateBuilder<DomainOperationManifestFailure>();
        var operationById = new Dictionary<string, DiscoveredDomainOperation>(StringComparer.Ordinal);
        var outcomeById = new Dictionary<string, DiscoveredDomainOperationOutcome>(StringComparer.Ordinal);
        var outcomeAttributes = new List<AttributeData>();

        foreach (var assembly in EnumerateAssemblies(compilation))
        {
            foreach (var attribute in assembly.GetAttributes())
            {
                var metadataName = attribute.AttributeClass?.ToDisplayString();
                if (metadataName == OperationAttributeMetadataName)
                {
                    if (!TryReadOperation(attribute, compilation, out var operation, out var failure))
                    {
                        if (failure is not null)
                            failures.Add(failure);
                        continue;
                    }

                    var operationId = operation!.Contract.OperationId;
                    if (operationById.TryGetValue(operationId, out var existing))
                    {
                        if (!OperationsEqual(existing.Contract, operation.Contract))
                        {
                            failures.Add(Failure(
                                DomainOperationManifestFailureKind.ConflictingOperation,
                                operationId,
                                $"Operation '{operationId}' is declared with conflicting signatures or bindings."));
                        }

                        continue;
                    }

                    operationById.Add(operationId, operation);
                    operations.Add(operation);
                }
                else if (metadataName == OutcomeAttributeMetadataName)
                {
                    outcomeAttributes.Add(attribute);
                }
            }
        }

        foreach (var attribute in outcomeAttributes)
        {
            if (!TryReadOutcome(attribute, compilation, operationById, out var outcome, out var failure))
            {
                if (failure is not null)
                    failures.Add(failure);
                continue;
            }

            var operationId = outcome!.Contract.OperationId;
            if (outcomeById.TryGetValue(operationId, out var existing))
            {
                if (!OutcomesEqual(existing.Contract, outcome.Contract))
                {
                    failures.Add(Failure(
                        DomainOperationManifestFailureKind.ConflictingOutcome,
                        operationId,
                        $"Operation '{operationId}' has conflicting outcome declarations."));
                }

                continue;
            }

            outcomeById.Add(operationId, outcome);
            outcomes.Add(outcome);
        }

        return new DomainOperationManifestExtraction(
            operations.ToImmutable(),
            outcomes.ToImmutable(),
            failures.ToImmutable());
    }

    private static IEnumerable<IAssemblySymbol> EnumerateAssemblies(Compilation compilation)
    {
        yield return compilation.Assembly;
        foreach (var reference in compilation.References)
        {
            if (compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assembly)
                yield return assembly;
        }
    }

    private static bool TryReadOperation(
        AttributeData attribute,
        Compilation compilation,
        out DiscoveredDomainOperation? operation,
        out DomainOperationManifestFailure? failure)
    {
        operation = null;
        failure = null;
        var arguments = attribute.ConstructorArguments;
        var operationId = arguments.Length > 2 ? arguments[2].Value as string ?? string.Empty : string.Empty;
        if (arguments.Length != 11 ||
            arguments[0].Value is not int schemaVersion ||
            arguments[1].Value is not INamedTypeSymbol sourceType ||
            arguments[2].Value is not string parsedOperationId ||
            arguments[3].Value is not string kindId ||
            arguments[4].Value is not INamedTypeSymbol declaringType ||
            arguments[5].Value is not INamedTypeSymbol subjectType ||
            arguments[6].Value is not string memberName ||
            arguments[7].Value is not ITypeSymbol returnType ||
            !TryReadStringArray(arguments[8], out var parameterNames) ||
            !TryReadTypeArray(arguments[9], out var parameterTypes) ||
            !TryReadStringArray(arguments[10], out var memberPaths))
        {
            failure = Failure(
                DomainOperationManifestFailureKind.MalformedOperationManifest,
                operationId,
                "The operation manifest constructor arguments are malformed or contain null arrays.");
            return false;
        }

        operationId = parsedOperationId;
        if (schemaVersion != DomainOperationContract.CurrentSchemaVersion)
        {
            failure = Failure(
                DomainOperationManifestFailureKind.UnsupportedOperationSchema,
                operationId,
                $"Operation '{operationId}' uses unsupported schema version {schemaVersion}.");
            return false;
        }

        if (parameterNames.Length != parameterTypes.Length || parameterNames.Length != memberPaths.Length)
        {
            failure = Failure(
                DomainOperationManifestFailureKind.MalformedOperationManifest,
                operationId,
                "ParameterNames, ParameterTypes, and MemberPaths must have the same length.");
            return false;
        }

        if (!IsSupportedOperationKind(kindId))
        {
            failure = Failure(
                DomainOperationManifestFailureKind.UnsupportedOperationKind,
                operationId,
                $"Operation kind '{kindId}' cannot be validated by this generator.");
            return false;
        }

        var parameters = new List<DomainOperationParameterContract>(parameterNames.Length);
        for (var index = 0; index < parameterNames.Length; index++)
        {
            try
            {
                parameters.Add(new DomainOperationParameterContract(
                    parameterNames[index],
                    Display(parameterTypes[index]),
                    memberPaths[index]));
            }
            catch (ArgumentException exception)
            {
                failure = Failure(
                    DomainOperationManifestFailureKind.MalformedOperationManifest,
                    operationId,
                    exception.Message);
                return false;
            }
        }

        var canonicalOperationId = DomainOperationContract.CreateOperationId(
            kindId,
            Display(declaringType),
            memberName,
            parameters);
        if (!string.Equals(operationId, canonicalOperationId, StringComparison.Ordinal))
        {
            failure = Failure(
                DomainOperationManifestFailureKind.InvalidOperationIdentity,
                operationId,
                $"Operation identifier '{operationId}' does not match canonical identifier '{canonicalOperationId}'.");
            return false;
        }

        if (!TryResolveCallable(
                kindId,
                declaringType,
                subjectType,
                memberName,
                returnType,
                parameterTypes,
                out var callableFailureKind,
                out var callableFailure))
        {
            failure = Failure(callableFailureKind, operationId, callableFailure);
            return false;
        }

        for (var index = 0; index < memberPaths.Length; index++)
        {
            if (!TryResolveReadableMemberPath(subjectType, memberPaths[index], out var memberType) ||
                !SymbolEqualityComparer.Default.Equals(memberType, parameterTypes[index]))
            {
                failure = Failure(
                    DomainOperationManifestFailureKind.InvalidParameterBinding,
                    operationId,
                    $"Parameter '{parameterNames[index]}' cannot bind to readable member path " +
                    $"'{Display(subjectType)}.{memberPaths[index]}' with type '{Display(parameterTypes[index])}'.");
                return false;
            }
        }

        try
        {
            var contract = new DomainOperationContract(
                operationId,
                kindId,
                Display(declaringType),
                Display(subjectType),
                memberName,
                Display(returnType),
                parameters,
                schemaVersion);
            operation = new DiscoveredDomainOperation(
                contract,
                Display(sourceType),
                location: null,
                returnType);
            return true;
        }
        catch (ArgumentException exception)
        {
            failure = Failure(
                DomainOperationManifestFailureKind.MalformedOperationManifest,
                operationId,
                exception.Message);
            return false;
        }
    }

    private static bool TryReadOutcome(
        AttributeData attribute,
        Compilation compilation,
        IReadOnlyDictionary<string, DiscoveredDomainOperation> operationById,
        out DiscoveredDomainOperationOutcome? outcome,
        out DomainOperationManifestFailure? failure)
    {
        outcome = null;
        failure = null;
        var arguments = attribute.ConstructorArguments;
        var operationId = arguments.Length > 3 ? arguments[3].Value as string ?? string.Empty : string.Empty;
        if (arguments.Length != 6 ||
            arguments[0].Value is not int schemaVersion ||
            arguments[1].Value is not INamedTypeSymbol sourceType ||
            arguments[2].Value is not INamedTypeSymbol subjectType ||
            arguments[3].Value is not string parsedOperationId ||
            arguments[4].Value is not string kindId ||
            !TryReadStringArray(arguments[5], out var parameters))
        {
            failure = Failure(
                DomainOperationManifestFailureKind.MalformedOutcomeManifest,
                operationId,
                "The operation-outcome manifest constructor arguments are malformed or contain a null parameter array.");
            return false;
        }

        operationId = parsedOperationId;
        if (schemaVersion != DomainOperationOutcomeContract.CurrentSchemaVersion)
        {
            failure = Failure(
                DomainOperationManifestFailureKind.UnsupportedOutcomeSchema,
                operationId,
                $"Operation outcome for '{operationId}' uses unsupported schema version {schemaVersion}.");
            return false;
        }

        if (!operationById.TryGetValue(operationId, out var operation) ||
            !string.Equals(operation.Contract.SubjectTypeName, Display(subjectType), StringComparison.Ordinal))
        {
            failure = Failure(
                DomainOperationManifestFailureKind.OrphanOutcome,
                operationId,
                $"Operation outcome '{operationId}' has no matching manifested operation for subject '{Display(subjectType)}'.");
            return false;
        }

        if (kindId == DomainOperationOutcomeKinds.ThrowsException &&
            (parameters.Length != 1 || !IsExceptionType(compilation, parameters[0])))
        {
            failure = Failure(
                DomainOperationManifestFailureKind.InvalidOutcome,
                operationId,
                "A throws-exception outcome must name exactly one resolvable System.Exception type.");
            return false;
        }

        if (kindId == DomainOperationOutcomeKinds.ReturnsResult &&
            (parameters.Length != 2 ||
             operation.Contract.ReturnTypeName == operation.Contract.SubjectTypeName))
        {
            failure = Failure(
                DomainOperationManifestFailureKind.InvalidOutcome,
                operationId,
                "A result outcome requires success/value member paths and a wrapper return type.");
            return false;
        }
        if (kindId == DomainOperationOutcomeKinds.ReturnsResult &&
            (operation.ReturnTypeSymbol is not INamedTypeSymbol wrapperType ||
             !TryResolveReadableMemberPath(wrapperType, parameters[0], out var successType) ||
             successType?.SpecialType != SpecialType.System_Boolean ||
             !TryResolveReadableMemberPath(wrapperType, parameters[1], out var valueType) ||
             !SymbolEqualityComparer.Default.Equals(valueType, subjectType)))
        {
            failure = Failure(
                DomainOperationManifestFailureKind.InvalidOutcome,
                operationId,
                "Result success must be a readable Boolean path and result value must be a readable subject path.");
            return false;
        }

        try
        {
            var contract = new DomainOperationOutcomeContract(operationId, kindId, parameters, schemaVersion);
            outcome = new DiscoveredDomainOperationOutcome(
                contract,
                Display(sourceType),
                Display(subjectType),
                location: null);
            return true;
        }
        catch (ArgumentException exception)
        {
            failure = Failure(
                DomainOperationManifestFailureKind.InvalidOutcome,
                operationId,
                exception.Message);
            return false;
        }
    }

    private static bool TryResolveCallable(
        string kindId,
        INamedTypeSymbol declaringType,
        INamedTypeSymbol subjectType,
        string memberName,
        ITypeSymbol returnType,
        ImmutableArray<ITypeSymbol> parameterTypes,
        out DomainOperationManifestFailureKind failureKind,
        out string failure)
    {
        failureKind = DomainOperationManifestFailureKind.CallableNotFound;
        failure = string.Empty;
        if (!IsPubliclyAccessible(declaringType) || !IsPubliclyAccessible(subjectType))
        {
            failure = "The manifested declaring and subject types must be publicly accessible.";
            return false;
        }

        IEnumerable<IMethodSymbol> candidates;
        if (kindId == DomainOperationKinds.Constructor)
        {
            if (memberName != ".ctor" || !SymbolEqualityComparer.Default.Equals(declaringType, subjectType))
            {
                failureKind = DomainOperationManifestFailureKind.SignatureMismatch;
                failure = "A constructor manifest must use '.ctor' and declare the subject type.";
                return false;
            }

            candidates = declaringType.InstanceConstructors;
        }
        else
        {
            candidates = EnumerateMethods(declaringType, memberName);
        }

        var namedCandidates = candidates.Where(method =>
                method.DeclaredAccessibility == Accessibility.Public &&
                method.TypeParameters.Length == 0 &&
                method.Parameters.Length == parameterTypes.Length &&
                method.Parameters.All(parameter => parameter.RefKind == RefKind.None) &&
                (kindId == DomainOperationKinds.StaticFactory
                    ? method.IsStatic && method.MethodKind == MethodKind.Ordinary
                    : kindId == DomainOperationKinds.InstanceCommand
                        ? !method.IsStatic && method.MethodKind == MethodKind.Ordinary
                        : method.MethodKind == MethodKind.Constructor))
            .ToArray();
        var signatureMatches = namedCandidates.Where(method => ParametersEqual(method.Parameters, parameterTypes)).ToArray();
        if (signatureMatches.Length == 0)
        {
            failureKind = namedCandidates.Length == 0
                ? DomainOperationManifestFailureKind.CallableNotFound
                : DomainOperationManifestFailureKind.SignatureMismatch;
            failure = $"No accessible callable '{Display(declaringType)}.{memberName}' matches the manifested parameter signature.";
            return false;
        }

        if (signatureMatches.Length > 1)
        {
            failureKind = DomainOperationManifestFailureKind.CallableAmbiguous;
            failure = $"Callable '{Display(declaringType)}.{memberName}' is ambiguous for the manifested parameter signature.";
            return false;
        }

        var method = signatureMatches[0];
        var actualReturnType = kindId == DomainOperationKinds.Constructor ? subjectType : method.ReturnType;
        if (!SymbolEqualityComparer.Default.Equals(actualReturnType, returnType))
        {
            failureKind = DomainOperationManifestFailureKind.ReturnTypeMismatch;
            failure = $"Callable '{Display(declaringType)}.{memberName}' does not return manifested type '{Display(returnType)}'.";
            return false;
        }

        if (kindId == DomainOperationKinds.InstanceCommand &&
            !IsSameOrBaseType(declaringType, subjectType))
        {
            failureKind = DomainOperationManifestFailureKind.SignatureMismatch;
            failure = $"Instance-command declaring type '{Display(declaringType)}' is not the subject type or one of its base types.";
            return false;
        }

        return true;
    }

    private static IEnumerable<IMethodSymbol> EnumerateMethods(INamedTypeSymbol type, string memberName)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            foreach (var method in current.GetMembers(memberName).OfType<IMethodSymbol>())
                yield return method;
        }
    }

    private static bool TryResolveReadableMemberPath(
        INamedTypeSymbol subjectType,
        string memberPath,
        out ITypeSymbol? memberType)
    {
        memberType = subjectType;
        if (string.IsNullOrWhiteSpace(memberPath))
            return false;

        foreach (var segment in memberPath.Split('.'))
        {
            if (memberType is not INamedTypeSymbol namedType)
                return false;

            IPropertySymbol? property = null;
            for (var current = namedType; current is not null && property is null; current = current.BaseType)
            {
                property = current.GetMembers(segment).OfType<IPropertySymbol>().SingleOrDefault(candidate =>
                    !candidate.IsStatic &&
                    candidate.Parameters.Length == 0 &&
                    candidate.GetMethod?.DeclaredAccessibility == Accessibility.Public);
            }

            if (property is null)
                return false;
            memberType = property.Type;
        }

        return true;
    }

    private static bool IsExceptionType(Compilation compilation, string typeName)
    {
        var metadataName = typeName.StartsWith("global::", StringComparison.Ordinal)
            ? typeName.Substring("global::".Length)
            : typeName;
        var type = compilation.GetTypeByMetadataName(metadataName);
        var exceptionType = compilation.GetTypeByMetadataName("System.Exception");
        if (type is null || exceptionType is null)
            return false;

        for (var current = type; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, exceptionType))
                return true;
        }

        return false;
    }

    private static bool IsSameOrBaseType(INamedTypeSymbol candidateBase, INamedTypeSymbol subjectType)
    {
        for (var current = subjectType; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, candidateBase))
                return true;
        }

        return false;
    }

    private static bool IsPubliclyAccessible(INamedTypeSymbol type)
    {
        for (var current = type; current is not null; current = current.ContainingType)
        {
            if (current.DeclaredAccessibility != Accessibility.Public)
                return false;
        }

        return true;
    }

    private static bool ParametersEqual(
        ImmutableArray<IParameterSymbol> parameters,
        ImmutableArray<ITypeSymbol> parameterTypes)
    {
        if (parameters.Length != parameterTypes.Length)
            return false;

        for (var index = 0; index < parameters.Length; index++)
        {
            if (!SymbolEqualityComparer.Default.Equals(parameters[index].Type, parameterTypes[index]))
                return false;
        }

        return true;
    }

    private static bool TryReadStringArray(TypedConstant constant, out ImmutableArray<string> values)
    {
        var builder = ImmutableArray.CreateBuilder<string>();
        if (constant.Kind != TypedConstantKind.Array || constant.IsNull)
        {
            values = default;
            return false;
        }

        foreach (var item in constant.Values)
        {
            if (item.Value is not string value)
            {
                values = default;
                return false;
            }

            builder.Add(value);
        }

        values = builder.ToImmutable();
        return true;
    }

    private static bool TryReadTypeArray(TypedConstant constant, out ImmutableArray<ITypeSymbol> values)
    {
        var builder = ImmutableArray.CreateBuilder<ITypeSymbol>();
        if (constant.Kind != TypedConstantKind.Array || constant.IsNull)
        {
            values = default;
            return false;
        }

        foreach (var item in constant.Values)
        {
            if (item.Value is not ITypeSymbol value)
            {
                values = default;
                return false;
            }

            builder.Add(value);
        }

        values = builder.ToImmutable();
        return true;
    }

    private static bool IsSupportedOperationKind(string kindId) =>
        kindId == DomainOperationKinds.Constructor ||
        kindId == DomainOperationKinds.StaticFactory ||
        kindId == DomainOperationKinds.InstanceCommand;

    private static bool OperationsEqual(DomainOperationContract left, DomainOperationContract right) =>
        left.SchemaVersion == right.SchemaVersion &&
        left.OperationId == right.OperationId &&
        left.KindId == right.KindId &&
        left.DeclaringTypeName == right.DeclaringTypeName &&
        left.SubjectTypeName == right.SubjectTypeName &&
        left.MemberName == right.MemberName &&
        left.ReturnTypeName == right.ReturnTypeName &&
        left.Parameters.Count == right.Parameters.Count &&
        left.Parameters.Zip(right.Parameters, (first, second) =>
                first.Name == second.Name &&
                first.TypeName == second.TypeName &&
                first.MemberPath == second.MemberPath)
            .All(equal => equal);

    private static bool OutcomesEqual(
        DomainOperationOutcomeContract left,
        DomainOperationOutcomeContract right) =>
        left.SchemaVersion == right.SchemaVersion &&
        left.OperationId == right.OperationId &&
        left.KindId == right.KindId &&
        left.Parameters.SequenceEqual(right.Parameters, StringComparer.Ordinal);

    private static string Display(ITypeSymbol type) =>
        type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    private static DomainOperationManifestFailure Failure(
        DomainOperationManifestFailureKind kind,
        string operationId,
        string message) =>
        new(kind, operationId, message);
}
