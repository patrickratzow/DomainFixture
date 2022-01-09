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
        _stringBuilder.Append(content);

        return this;
    }

    public CodeBuilder AppendLine(string content)
    {
        _stringBuilder.AppendLine(content);

        return this;
    }

    public override string ToString()
    {
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
}