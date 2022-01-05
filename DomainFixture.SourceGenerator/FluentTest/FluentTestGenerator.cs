using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace DomainFixture.SourceGenerator;

[Generator]
public class FluentTestGenerator : ISourceGenerator
{
    private string _code;
    private GeneratorExecutionContext _context;

    public void Initialize(GeneratorInitializationContext context)
    {
        // Register a syntax receiver that will be created for each generation pass
        context.RegisterForSyntaxNotifications(() => new FluentTestSyntaxReceiver());
    }
    
    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxReceiver is not FluentTestSyntaxReceiver receiver) return;
        _context = context;

        var classSymbols = GetClassSymbolsWithTheRightAttribute(receiver);
        foreach (var classSymbol in classSymbols)
        {
            var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
            var className = $"I{classSymbol.Name}";
            var sourceBuilder = new FluentTestBuilder(_context, classSymbol, namespaceName, className);

            AddSource(sourceBuilder, className);
        }
    }

    private List<INamedTypeSymbol> GetClassSymbolsWithTheRightAttribute(FluentTestSyntaxReceiver receiver)
    {
        var compilation = _context.Compilation;
        var attributeSymbol =
            compilation.GetTypeByMetadataName(typeof(GenerateFixtureTests).Namespace + "." + nameof(GenerateFixtureTests));
        
        var classSymbols = new List<INamedTypeSymbol>();
        foreach (var cls in receiver.Candidates)
        {
            var model = compilation.GetSemanticModel(cls.SyntaxTree);
            var classSymbol = model.GetDeclaredSymbol(cls);
            var hasFluentTestAttribute = classSymbol!.GetAttributes()
                .Any(ad => ad.AttributeClass!.Name == attributeSymbol!.Name);
            if (!hasFluentTestAttribute) continue;

            classSymbols.Add((INamedTypeSymbol) classSymbol);
        }

        return classSymbols;
    }
    
    private void AddSource(FluentTestBuilder builder, string className)
    {
        var descriptor = new DiagnosticDescriptor(nameof(FluentTestGenerator), "Result", 
            $"Finished compilation for {className}", "Compilation", DiagnosticSeverity.Info, true);
        _context.ReportDiagnostic(Diagnostic.Create(descriptor, null));

        //if (!Debugger.IsAttached) Debugger.Launch();
        
        // inject the created source into the users compilation
        _context.AddSource($"{className}.generated.cs", SourceText.From(builder.ToString(), Encoding.UTF8));
    }
}