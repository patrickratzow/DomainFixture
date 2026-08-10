using System;
using System.Collections.Generic;
using System.Linq;

namespace DomainFixture.TestGenerator.Model;

/// <summary>
/// A collection of tests that will be emitted into one generated class.
/// </summary>
public sealed class GeneratedTestSuite
{
    public string NamespaceName { get; }
    public string ClassName { get; }
    public IReadOnlyList<GeneratedTest> Tests { get; }

    public GeneratedTestSuite(string namespaceName, string className, IEnumerable<GeneratedTest> tests)
    {
        if (string.IsNullOrWhiteSpace(namespaceName))
            throw new ArgumentException("A generated test suite must have a namespace.", nameof(namespaceName));
        if (string.IsNullOrWhiteSpace(className))
            throw new ArgumentException("A generated test suite must have a class name.", nameof(className));
        if (tests is null)
            throw new ArgumentNullException(nameof(tests));

        NamespaceName = namespaceName;
        ClassName = className;
        Tests = tests.ToArray();

        var duplicateName = Tests
            .GroupBy(test => test.Name, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1)
            ?.Key;

        if (duplicateName is not null)
            throw new ArgumentException($"The generated test name '{duplicateName}' is duplicated.", nameof(tests));
    }
}
