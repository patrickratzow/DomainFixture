using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DomainFixture.SourceGenerator;

public class FluentTestSyntaxReceiver : ISyntaxReceiver
{
    public List<ClassDeclarationSyntax> Candidates { get; } = new();

    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    {
        if (syntaxNode is not ClassDeclarationSyntax {AttributeLists.Count: > 0} classDeclarationSyntax) return;
                
        Candidates.Add(classDeclarationSyntax);
    }
}