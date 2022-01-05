using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace DomainFixture.SourceGenerator;

public abstract class CodeBuilder
{
    protected readonly GeneratorExecutionContext Context;
    private readonly List<string> _using = new();
    private int _currentIndent;
    private StringBuilder _stringBuilder = null!;

    protected CodeBuilder(GeneratorExecutionContext context)
    {
        Context = context;
    }
    
    public abstract string NamespaceName { get; protected set; }
    public abstract string ClassName { get; protected set; }

    protected virtual void BeforeGenerated()
    {
    }

    protected virtual void AfterGenerated()
    {
    }

    protected string PrintIndent() => new(' ', _currentIndent * 4);

    protected void Indent()
    {
        _currentIndent++;
    }

    protected void Outdent()
    {
        _currentIndent--;
    }

    protected void Use(string name)
    {
        _using.Add(name);
    }

    protected void AppendLine(string line = "")
    {
        _stringBuilder.AppendLine($"{PrintIndent()}{line}");
    }

    private void WriteUsing()
    {
        if (_using.Count <= 0) return;

        foreach (var name in _using)
        {
            AppendLine($"using {name};");
        }

        AppendLine();
    }

    protected abstract void WriteCode();

    public override string ToString()
    {
        _stringBuilder = new();

        BeforeGenerated();

        WriteUsing();
        AppendLine($"namespace {NamespaceName}");
        AppendLine("{");
        Indent();
        AppendLine($"public partial class {ClassName}");
        AppendLine("{");
        Indent();
        WriteCode();
        Outdent();
        AppendLine("}");
        Outdent();
        AppendLine("}");

        AfterGenerated();

        return _stringBuilder.ToString();
    }
}