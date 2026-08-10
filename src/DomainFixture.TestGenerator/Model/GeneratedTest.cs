using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DomainFixture.TestGenerator.Model;

/// <summary>
/// A framework-neutral description of one generated test method.
/// </summary>
public sealed class GeneratedTest
{
    public string Name { get; }
    public IReadOnlyList<StatementSyntax> Statements { get; }
    public bool IsAsync { get; }

    public GeneratedTest(string name, IEnumerable<StatementSyntax> statements, bool isAsync = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("A generated test must have a name.", nameof(name));
        if (statements is null)
            throw new ArgumentNullException(nameof(statements));

        Name = name;
        Statements = statements.ToArray();
        IsAsync = isAsync;
    }
}
