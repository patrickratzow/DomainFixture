using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DomainFixture.Contracts;
using DomainFixture.Generation.Metadata;
using DomainFixture.SourceGenerator;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

namespace DomainFixture.SourceGenerator.Tests;

[TestFixture]
public sealed class DomainOperationManifestProviderTests
{
    [Test]
    public void Extract_ShouldReadOverloadedFactoryAndExceptionOutcome()
    {
        var extraction = Extract(ValidManifestSource);

        extraction.Failures.Should().BeEmpty();
        var operation = extraction.Operations.Should().ContainSingle().Which;
        operation.OperationId.Should().Be(OperationId);
        operation.MemberName.Should().Be("Parse");
        operation.ReturnTypeName.Should().Be("global::External.ValueName");
        operation.Parameters.Should().ContainSingle().Which.MemberPath.Should().Be("Value");
        var outcome = extraction.Outcomes.Should().ContainSingle().Which;
        outcome.OperationId.Should().Be(OperationId);
        outcome.KindId.Should().Be(DomainOperationOutcomeKinds.ThrowsException);
        outcome.Parameters.Should().Equal("System.ArgumentException");
    }

    [Test]
    public void Extract_ShouldReadConstructorAndParameterlessInstanceCommand()
    {
        var extraction = Extract(ConstructorAndCommandSource);

        extraction.Failures.Should().BeEmpty();
        extraction.Operations.Should().HaveCount(2);
        extraction.Operations.Should().Contain(operation =>
            operation.KindId == DomainOperationKinds.Constructor &&
            operation.Parameters.Count == 1);
        extraction.Operations.Should().Contain(operation =>
            operation.KindId == DomainOperationKinds.InstanceCommand &&
            operation.Parameters.Count == 0 &&
            operation.ReturnTypeName == "void");
    }

    [Test]
    public void Extract_ShouldValidateGenericResultOutcomePaths()
    {
        var valid = Extract(ResultManifestSource);
        var invalid = Extract(ResultManifestSource.Replace(
            "new[] { \"IsSuccess\", \"Value\" })]",
            "new[] { \"Error\", \"Value\" })]"));

        valid.Failures.Should().BeEmpty();
        valid.Operations.Should().ContainSingle().Which.ReturnTypeName.Should()
            .Be("global::External.Result<global::External.ValueName>");
        valid.Outcomes.Should().ContainSingle().Which.KindId.Should()
            .Be(DomainOperationOutcomeKinds.ReturnsResult);
        invalid.Failures.Should().ContainSingle(failure => failure.Kind == "InvalidOutcome");
    }

    [Test]
    public void Extract_ShouldRejectMismatchedParallelParameterArrays()
    {
        var extraction = Extract(ValidManifestSource.Replace(
            "new[] { \"Value\" })]",
            "new string[0])]"));

        extraction.Operations.Should().BeEmpty();
        extraction.Outcomes.Should().BeEmpty();
        extraction.Failures.Should().ContainSingle(failure =>
            failure.Kind == "MalformedOperationManifest" &&
            failure.Message.Contains("same length"));
    }

    [Test]
    public void Extract_ShouldRejectStaleCallableAndReturnMetadata()
    {
        var missingCallable = Extract(OperationOnlySource
            .Replace(OperationId, MissingOperationId)
            .Replace("\"Parse\",\n    typeof(External.ValueName)", "\"Missing\",\n    typeof(External.ValueName)"));
        var wrongReturn = Extract(OperationOnlySource.Replace(
            "\"Parse\",\n    typeof(External.ValueName)",
            "\"Parse\",\n    typeof(string)"));

        missingCallable.Failures.Should().ContainSingle(failure => failure.Kind == "CallableNotFound");
        wrongReturn.Failures.Should().ContainSingle(failure => failure.Kind == "ReturnTypeMismatch");
    }

    [Test]
    public void Extract_ShouldRejectInvalidBindingAndNonCanonicalIdentity()
    {
        var invalidBinding = Extract(OperationOnlySource.Replace(
            "new[] { \"Value\" })]",
            "new[] { \"Missing\" })]"));
        var invalidIdentity = Extract(OperationOnlySource.Replace(OperationId, "external.parse"));

        invalidBinding.Failures.Should().ContainSingle(failure => failure.Kind == "InvalidParameterBinding");
        invalidIdentity.Failures.Should().ContainSingle(failure => failure.Kind == "InvalidOperationIdentity");
    }

    [Test]
    public void Extract_ShouldReportUnsupportedSchemasAndOrphanOutcomes()
    {
        var operationSchema = Extract(OperationOnlySource.Replace(
            "DomainOperationManifest(\n    2,",
            "DomainOperationManifest(\n    99,"));
        var outcomeSchema = Extract(ValidManifestSource.Replace(
            "DomainOperationOutcomeManifest(\n    1,",
            "DomainOperationOutcomeManifest(\n    99,"));
        var orphan = Extract(OutcomeOnlySource);

        operationSchema.Failures.Should().ContainSingle(failure => failure.Kind == "UnsupportedOperationSchema");
        outcomeSchema.Failures.Should().ContainSingle(failure => failure.Kind == "UnsupportedOutcomeSchema");
        orphan.Failures.Should().ContainSingle(failure => failure.Kind == "OrphanOutcome");
    }

    [Test]
    public void Extract_ShouldDeduplicateIdenticalOperationsAndRejectConflictingBindings()
    {
        var identical = Extract(OperationOnlySource.Replace(
            "namespace External",
            OperationAttribute + Environment.NewLine + Environment.NewLine + "namespace External"));
        var conflicting = Extract(OperationOnlySource.Replace(
            "namespace External",
            OperationAttribute.Replace("new[] { \"Value\" })]", "new[] { \"Alias\" })]") +
            Environment.NewLine + Environment.NewLine + "namespace External"));

        identical.Failures.Should().BeEmpty();
        identical.Operations.Should().ContainSingle();
        conflicting.Operations.Should().ContainSingle();
        conflicting.Failures.Should().ContainSingle(failure => failure.Kind == "ConflictingOperation");
    }

    private static ExtractionView Extract(string manifestSource)
    {
        var reference = CompileReference(manifestSource);
        var compilation = CSharpCompilation.Create(
            $"ManifestConsumer_{Guid.NewGuid():N}",
            new[] { CSharpSyntaxTree.ParseText("public sealed class Consumer { }") },
            CreateReferences().Concat(new[] { reference }),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var providerType = typeof(DomainFixtureIncrementalGenerator).Assembly.GetType(
            "DomainFixture.SourceGenerator.Extraction.DomainOperationManifestProvider",
            throwOnError: true)!;
        var extract = providerType.GetMethod(
            "Extract",
            BindingFlags.Static | BindingFlags.NonPublic)!;
        var result = extract.Invoke(null, new object[] { compilation })!;

        return ExtractionView.From(result);
    }

    private static MetadataReference CompileReference(string source)
    {
        var compilation = CSharpCompilation.Create(
            $"ExternalManifest_{Guid.NewGuid():N}",
            new[] { CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp10)) },
            CreateReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        var emitResult = compilation.Emit(stream);
        emitResult.Success.Should().BeTrue(string.Join(Environment.NewLine, emitResult.Diagnostics));
        return MetadataReference.CreateFromImage(stream.ToArray());
    }

    private static IEnumerable<MetadataReference> CreateReferences()
    {
        var references = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))!
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path))
            .ToList();
        references.Add(MetadataReference.CreateFromFile(typeof(DomainOperationContract).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(DomainOperationManifestAttribute).Assembly.Location));
        return references;
    }

    private sealed class ExtractionView
    {
        public IReadOnlyList<DomainOperationContract> Operations { get; }
        public IReadOnlyList<DomainOperationOutcomeContract> Outcomes { get; }
        public IReadOnlyList<FailureView> Failures { get; }

        private ExtractionView(
            IReadOnlyList<DomainOperationContract> operations,
            IReadOnlyList<DomainOperationOutcomeContract> outcomes,
            IReadOnlyList<FailureView> failures)
        {
            Operations = operations;
            Outcomes = outcomes;
            Failures = failures;
        }

        public static ExtractionView From(object extraction)
        {
            var type = extraction.GetType();
            var operations = Items(type.GetProperty("Operations")!.GetValue(extraction)!)
                .Select(item => (DomainOperationContract)item.GetType().GetProperty("Contract")!.GetValue(item)!)
                .ToArray();
            var outcomes = Items(type.GetProperty("Outcomes")!.GetValue(extraction)!)
                .Select(item => (DomainOperationOutcomeContract)item.GetType().GetProperty("Contract")!.GetValue(item)!)
                .ToArray();
            var failures = Items(type.GetProperty("Failures")!.GetValue(extraction)!)
                .Select(FailureView.From)
                .ToArray();
            return new ExtractionView(operations, outcomes, failures);
        }

        private static IEnumerable<object> Items(object value) => ((IEnumerable)value).Cast<object>();
    }

    private sealed class FailureView
    {
        public string Kind { get; }
        public string OperationId { get; }
        public string Message { get; }

        private FailureView(string kind, string operationId, string message)
        {
            Kind = kind;
            OperationId = operationId;
            Message = message;
        }

        public static FailureView From(object failure)
        {
            var type = failure.GetType();
            return new FailureView(
                type.GetProperty("Kind")!.GetValue(failure)!.ToString()!,
                (string)type.GetProperty("OperationId")!.GetValue(failure)!,
                (string)type.GetProperty("Message")!.GetValue(failure)!);
        }
    }

    private const string OperationId =
        "domainfixture.operation.static-factory:global::External.ValueName.Parse(string)";
    private const string MissingOperationId =
        "domainfixture.operation.static-factory:global::External.ValueName.Missing(string)";

    private const string OperationAttribute = @"
[assembly: DomainOperationManifest(
    2,
    typeof(External.Adapter),
    ""domainfixture.operation.static-factory:global::External.ValueName.Parse(string)"",
    ""domainfixture.operation.static-factory"",
    typeof(External.ValueName),
    typeof(External.ValueName),
    ""Parse"",
    typeof(External.ValueName),
    new[] { ""value"" },
    new[] { typeof(string) },
    new[] { ""Value"" })]";

    private const string OperationOnlySource = @"
using DomainFixture.Generation.Metadata;
" + OperationAttribute + @"

namespace External
{
    public sealed class ValueName
    {
        private ValueName(string value)
        {
            Value = value;
            Alias = value;
        }

        public string Value { get; }
        public string Alias { get; }
        public static ValueName Parse(string value) => new(value);
        public static ValueName Parse(int value) => new(value.ToString());
    }

    public sealed class Adapter { }
}";

    private const string OutcomeAttribute = @"
[assembly: DomainOperationOutcomeManifest(
    1,
    typeof(External.Adapter),
    typeof(External.ValueName),
    ""domainfixture.operation.static-factory:global::External.ValueName.Parse(string)"",
    ""domainfixture.operation-outcome.throws-exception"",
    new[] { ""System.ArgumentException"" })]";

    private const string OutcomeOnlySource = @"
using DomainFixture.Generation.Metadata;
" + OutcomeAttribute + @"

namespace External
{
    public sealed class ValueName { }
    public sealed class Adapter { }
}";

    private const string ConstructorAndCommandSource = @"
using DomainFixture.Generation.Metadata;

[assembly: DomainOperationManifest(
    2,
    typeof(External.Adapter),
    ""domainfixture.operation.constructor:global::External.ValueName..ctor(string)"",
    ""domainfixture.operation.constructor"",
    typeof(External.ValueName),
    typeof(External.ValueName),
    "".ctor"",
    typeof(External.ValueName),
    new[] { ""value"" },
    new[] { typeof(string) },
    new[] { ""Value"" })]

[assembly: DomainOperationManifest(
    2,
    typeof(External.Adapter),
    ""domainfixture.operation.instance-command:global::External.ValueName.Approve()"",
    ""domainfixture.operation.instance-command"",
    typeof(External.ValueName),
    typeof(External.ValueName),
    ""Approve"",
    typeof(void),
    new string[0],
    new System.Type[0],
    new string[0])]

namespace External
{
    public sealed class ValueName
    {
        public ValueName(string value) => Value = value;
        public string Value { get; }
        public void Approve() { }
    }

    public sealed class Adapter { }
}";

    private const string ResultManifestSource = @"
using DomainFixture.Generation.Metadata;

[assembly: DomainOperationManifest(
    2,
    typeof(External.Adapter),
    ""domainfixture.operation.static-factory:global::External.ValueName.Create(string)"",
    ""domainfixture.operation.static-factory"",
    typeof(External.ValueName),
    typeof(External.ValueName),
    ""Create"",
    typeof(External.Result<External.ValueName>),
    new[] { ""value"" },
    new[] { typeof(string) },
    new[] { ""Value"" })]

[assembly: DomainOperationOutcomeManifest(
    1,
    typeof(External.Adapter),
    typeof(External.ValueName),
    ""domainfixture.operation.static-factory:global::External.ValueName.Create(string)"",
    ""domainfixture.operation-outcome.result"",
    new[] { ""IsSuccess"", ""Value"" })]

namespace External
{
    public sealed class Result<T>
    {
        public bool IsSuccess { get; set; }
        public string Error { get; set; } = string.Empty;
        public T Value { get; set; } = default!;
    }

    public sealed class ValueName
    {
        public string Value { get; set; } = string.Empty;
        public static Result<ValueName> Create(string value) => new();
    }

    public sealed class Adapter { }
}";

    private static readonly string ValidManifestSource = OperationOnlySource.Replace(
        "namespace External",
        OutcomeAttribute + Environment.NewLine + Environment.NewLine + "namespace External");
}
