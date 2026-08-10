using System.Text;

namespace DomainFixture.TestGenerator;

public abstract class CodeBuilder
{
    private readonly StringBuilder _stringBuilder = new();
    private int _indentationLevel;
    protected abstract void Write();

    public Indentation Indent()
    {
        _indentationLevel++;
        
        return new(this);
    }

    public CodeBuilder Outdent()
    {
        _indentationLevel--;

        return this;
    }

    public CodeBuilder Append(string content)
    {
        _stringBuilder.Append(WithIndent(content));

        return this;
    }

    public CodeBuilder AppendLine(string content = "")
    {
        _stringBuilder.Append(WithIndent(content));
        _stringBuilder.Append('\n');

        return this;
    }

    public override string ToString()
    {
        _stringBuilder.Clear();
        _indentationLevel = 0;
        BeforeWrite();
        Write();
        AfterWrite();
        
        return _stringBuilder.ToString();
    }

    protected virtual void BeforeWrite()
    {
    }

    protected virtual void AfterWrite()
    {
    }

    private string WithIndent(string content) => new string(' ', _indentationLevel * 4) + content;
}
