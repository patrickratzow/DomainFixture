using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DomainFixture.Contracts;
using DomainFixture.SourceGenerator;
using DomainFixture.Tests.Domain.Entities;
using DomainFixture.Validation;
using FluentAssertions;
using FluentValidation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

namespace DomainFixture.SourceGenerator.Tests;

[TestFixture]
public sealed class DomainGeneratedSuiteSnapshotTests
{
    [Test]
    public void Domain_suites_should_match_visible_generated_sources()
    {
        var sourceDirectory = TestContext.CurrentContext.TestDirectory;
        var sources = new[]
        {
            File.ReadAllText(Path.Combine(sourceDirectory, "Inputs", "DomainFixtureProfile.cs")),
            File.ReadAllText(Path.Combine(sourceDirectory, "Inputs", "RegistrationRequestFixture.cs")),
            File.ReadAllText(Path.Combine(sourceDirectory, "Inputs", "UsernameFixture.cs")),
            File.ReadAllText(Path.Combine(sourceDirectory, "Inputs", "QualifiedNameFixture.cs")),
            File.ReadAllText(Path.Combine(sourceDirectory, "Inputs", "QualifiedHandleFixture.cs")),
            File.ReadAllText(Path.Combine(sourceDirectory, "Inputs", "RegistrationFixture.cs")),
            File.ReadAllText(Path.Combine(sourceDirectory, "Inputs", "SubscriptionFixture.cs")),
            File.ReadAllText(Path.Combine(sourceDirectory, "Inputs", "ResultDisplayNameFixture.cs")),
            File.ReadAllText(Path.Combine(sourceDirectory, "Inputs", "TenantSubscriptionFixture.cs"))
        };
        var compilation = CSharpCompilation.Create(
            $"DomainSnapshot_{Guid.NewGuid():N}",
            sources.Select(source => CSharpSyntaxTree.ParseText(
                source,
                new CSharpParseOptions(LanguageVersion.CSharp10))),
            CreateReferences(),
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new DomainFixtureIncrementalGenerator().AsSourceGenerator());

        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var outputCompilation,
            out _);

        var result = driver.GetRunResult();
        result.Diagnostics.Should().BeEmpty();
        outputCompilation.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Should().BeEmpty();

        var snapshots = new[]
        {
            (
                HintName: "RegistrationRequestFixture.Registration.g.cs",
                RelativePath: "Snapshots/DomainFixture.Tests.Domain/RegistrationRequestFixture.Registration.verified.cs"),
            (
                HintName: "UsernameFixture.Validation.g.cs",
                RelativePath: "Snapshots/DomainFixture.Tests.Domain/UsernameFixture.Validation.verified.cs"),
            (
                HintName: "QualifiedNameFixture.Validation.g.cs",
                RelativePath: "Snapshots/DomainFixture.Tests.Domain/QualifiedNameFixture.Validation.verified.cs"),
            (
                HintName: "QualifiedHandleFixture.Validation.g.cs",
                RelativePath: "Snapshots/DomainFixture.Tests.Domain/QualifiedHandleFixture.Validation.verified.cs"),
            (
                HintName: "UsernameFixture.Factory.g.cs",
                RelativePath: "Snapshots/DomainFixture.Tests.Domain/UsernameFixture.Factory.verified.cs"),
            (
                HintName: "RegistrationFixture.Pending.g.cs",
                RelativePath: "Snapshots/DomainFixture.Tests.Domain/RegistrationFixture.Pending.verified.cs"),
            (
                HintName: "RegistrationFixture.Approved.g.cs",
                RelativePath: "Snapshots/DomainFixture.Tests.Domain/RegistrationFixture.Approved.verified.cs"),
            (
                HintName: "RegistrationFixture.Factory.g.cs",
                RelativePath: "Snapshots/DomainFixture.Tests.Domain/RegistrationFixture.Factory.verified.cs"),
            (
                HintName: "SubscriptionFixture.Valid.g.cs",
                RelativePath: "Snapshots/DomainFixture.Tests.Domain/SubscriptionFixture.Valid.verified.cs"),
            (
                HintName: "SubscriptionFixture.Factory.g.cs",
                RelativePath: "Snapshots/DomainFixture.Tests.Domain/SubscriptionFixture.Factory.verified.cs"),
            (
                HintName: "ResultDisplayNameFixture.Valid.g.cs",
                RelativePath: "Snapshots/DomainFixture.Tests.Domain/ResultDisplayNameFixture.Valid.verified.cs"),
            (
                HintName: "ResultDisplayNameFixture.Factory.g.cs",
                RelativePath: "Snapshots/DomainFixture.Tests.Domain/ResultDisplayNameFixture.Factory.verified.cs"),
            (
                HintName: "TenantSubscriptionFixture.Valid.g.cs",
                RelativePath: "Snapshots/DomainFixture.Tests.Domain/TenantSubscriptionFixture.Valid.verified.cs"),
            (
                HintName: "TenantSubscriptionFixture.Factory.g.cs",
                RelativePath: "Snapshots/DomainFixture.Tests.Domain/TenantSubscriptionFixture.Factory.verified.cs")
        };

        foreach (var expected in snapshots)
        {
            var generatedSource = NormalizeNewlines(result.GeneratedTrees
                .Single(tree => HasHintName(tree.FilePath, expected.HintName))
                .ToString());
            if (string.Equals(
                    Environment.GetEnvironmentVariable("UPDATE_DOMAIN_FIXTURE_SNAPSHOTS"),
                    "1",
                    StringComparison.Ordinal))
            {
                var projectDirectory = Path.GetFullPath(Path.Combine(
                    sourceDirectory,
                    "..",
                    "..",
                    ".."));
                var sourceSnapshotPath = Path.Combine(
                    projectDirectory,
                    expected.RelativePath.Replace('/', Path.DirectorySeparatorChar));
                File.WriteAllText(sourceSnapshotPath, generatedSource);
                continue;
            }

            var snapshotPath = Path.Combine(
                sourceDirectory,
                expected.RelativePath.Replace('/', Path.DirectorySeparatorChar));

            if (!File.Exists(snapshotPath))
            {
                Assert.Fail(
                    $"Visible generated-source snapshot is missing at '{snapshotPath}'." +
                    $"{Environment.NewLine}{Environment.NewLine}{generatedSource}");
            }

            var snapshot = NormalizeNewlines(File.ReadAllText(snapshotPath));
            AssertSnapshot(snapshot, generatedSource, snapshotPath);
        }
    }

    private static IEnumerable<MetadataReference> CreateReferences()
    {
        var references = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))!
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path))
            .ToList();
        references.Add(MetadataReference.CreateFromFile(typeof(IFixtureValidator<>).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(DomainConstraintContract).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(AbstractValidator<>).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(RegistrationRequest).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(TestAttribute).Assembly.Location));

        return references;
    }

    private static bool HasHintName(string filePath, string hintName)
    {
        if (!filePath.EndsWith(hintName, StringComparison.Ordinal))
            return false;
        var start = filePath.Length - hintName.Length;
        return start == 0 || filePath[start - 1] is '.' or '/' or '\\';
    }

    private static void AssertSnapshot(string expected, string actual, string snapshotPath)
    {
        if (expected == actual)
            return;

        var expectedLines = expected.Split('\n');
        var actualLines = actual.Split('\n');
        var maximum = Math.Max(expectedLines.Length, actualLines.Length);
        var mismatch = Enumerable.Range(0, maximum).First(index =>
            index >= expectedLines.Length ||
            index >= actualLines.Length ||
            expectedLines[index] != actualLines[index]);
        var expectedLine = mismatch < expectedLines.Length ? expectedLines[mismatch] : "<end of file>";
        var actualLine = mismatch < actualLines.Length ? actualLines[mismatch] : "<end of file>";

        Assert.Fail(
            $"Generated .Domain suite changed at line {mismatch + 1}." +
            $"{Environment.NewLine}Snapshot: {snapshotPath}" +
            $"{Environment.NewLine}Expected: {expectedLine}" +
            $"{Environment.NewLine}Actual:   {actualLine}" +
            $"{Environment.NewLine}{Environment.NewLine}Generated source:" +
            $"{Environment.NewLine}{actual}");
    }

    private static string NormalizeNewlines(string value) =>
        value.Replace("\r\n", "\n").TrimEnd() + "\n";
}
