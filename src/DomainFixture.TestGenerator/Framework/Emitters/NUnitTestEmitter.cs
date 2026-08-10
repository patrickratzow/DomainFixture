using System;
using System.Collections.Generic;
using System.CodeDom.Compiler;
using System.Linq;
using System.Text;
using DomainFixture.TestGenerator.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace DomainFixture.TestGenerator.Framework.Emitters;

/// <summary>
/// Emits deterministic NUnit source from a typed generated-test model.
/// </summary>
public sealed class NUnitTestEmitter
{
    private const string GeneratorName = "DomainFixture.TestGenerator";

    public CompilationUnitSyntax Emit(GeneratedTestSuite suite)
    {
        if (suite is null) throw new ArgumentNullException(nameof(suite));

        var usings = new List<UsingDirectiveSyntax>
        {
            UsingDirective(ParseName(typeof(GeneratedCodeAttribute).Namespace!)),
            UsingDirective(ParseName("NUnit.Framework"))
        };

        if (suite.Tests.Any(test => test.IsAsync))
            usings.Add(UsingDirective(ParseName("System.Threading.Tasks")));

        var testClass = ClassDeclaration(CreateIdentifier(suite.ClassName))
            .AddAttributeLists(
                AttributeList(SingletonSeparatedList(
                    Attribute(IdentifierName(nameof(GeneratedCodeAttribute).Replace("Attribute", string.Empty)))
                        .AddArgumentListArguments(
                            AttributeArgument(LiteralExpression(
                                SyntaxKind.StringLiteralExpression,
                                Literal(GeneratorName))),
                            AttributeArgument(LiteralExpression(
                                SyntaxKind.StringLiteralExpression,
                                Literal(GetGeneratorVersion())))))),
                AttributeList(SingletonSeparatedList(Attribute(IdentifierName("TestFixture")))))
            .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.SealedKeyword))
            .AddMembers(suite.Tests.Select(EmitTest).ToArray());

        var @namespace = NamespaceDeclaration(ParseName(suite.NamespaceName))
            .AddMembers(testClass);

        return CompilationUnit()
            .AddUsings(usings.ToArray())
            .AddMembers(@namespace);
    }

    public string EmitSource(GeneratedTestSuite suite)
    {
        return Emit(suite)
            .NormalizeWhitespace(indentation: "    ", eol: "\n")
            .ToFullString() + "\n";
    }

    private static MethodDeclarationSyntax EmitTest(GeneratedTest test)
    {
        var method = MethodDeclaration(
                test.IsAsync ? IdentifierName("Task") : PredefinedType(Token(SyntaxKind.VoidKeyword)),
                CreateIdentifier(test.Name))
            .AddAttributeLists(AttributeList(SingletonSeparatedList(Attribute(IdentifierName("Test")))))
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .WithBody(Block(test.Statements));

        if (test.IsAsync)
            method = method.AddModifiers(Token(SyntaxKind.AsyncKeyword));

        return method;
    }

    private static SyntaxToken CreateIdentifier(string value)
    {
        var builder = new StringBuilder(value.Length + 1);

        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            var valid = index == 0
                ? SyntaxFacts.IsIdentifierStartCharacter(character)
                : SyntaxFacts.IsIdentifierPartCharacter(character);

            builder.Append(valid ? character : '_');
        }

        if (builder.Length == 0 || !SyntaxFacts.IsIdentifierStartCharacter(builder[0]))
            builder.Insert(0, '_');

        var identifier = builder.ToString();
        if (SyntaxFacts.GetKeywordKind(identifier) != SyntaxKind.None ||
            SyntaxFacts.GetContextualKeywordKind(identifier) != SyntaxKind.None)
        {
            identifier = "@" + identifier;
        }

        return Identifier(identifier);
    }

    private static string GetGeneratorVersion()
    {
        return typeof(NUnitTestEmitter).Assembly.GetName().Version?.ToString() ?? "0.0.0.0";
    }
}
