using System.Collections.Generic;
using System.Linq;
using DomainFixture.TestGenerator.Framework;

namespace DomainFixture.TestGenerator;

public class FileCodeWriter<TClass> : CodeBuilder
{
    private readonly FrameworkAttributes _frameworkAttributes;
    private readonly List<ITestWriter> _testWriters = new();
    public string ClassName => $"{typeof(TClass).Name}Tests";
    public string NamespaceName { get; }

    public FileCodeWriter(string namespaceName, TestingFramework framework)
    {
        NamespaceName = namespaceName;
        
        var strategy = FrameworkStrategyFactory.CreateStrategy(framework);
        _frameworkAttributes = strategy.CreateAttributes();
    }

    public void AddTestWriter(ITestWriter writer)
    {
        _testWriters.Add(writer);
    }
    
    protected override void BeforeWrite()
    {
        var namespaces = _testWriters.Select(x => x.Namespaces).Distinct();
        foreach (var @namespace in namespaces)
        {
            AppendLine($"using {@namespace};");
        }
        AppendLine("using System.CodeDom.Compiler;");
        AppendLine("using DomainFixture;");
        AppendLine();
    }

    protected override void Write()
    {
        AppendLine($"namespace {NamespaceName}");
        AppendLine("{");
        using (var _ = Indent())
        {
            WriteClass();
        }
        AppendLine("}");
    }

    private void WriteClass()
    {
        // TODO: Use some Attribute for this instead
        var version = typeof(FileCodeWriter<>).Assembly.GetName().Version.ToString();
        AppendLine($"[GeneratedCode(\"DomainFixture.TestGenerator\", \"{version}\")]");
        
        var classAttribute = _frameworkAttributes.ClassAttribute;
        if (classAttribute is not null)
            AppendLine(classAttribute.ToString());

        AppendLine($"public class {ClassName}");
        AppendLine("{");
        using (var lvl2 = Indent())
        {
            WriteClassBody();
        }
        AppendLine("}");
    }

    private void WriteClassBody()
    {
        var tests = _testWriters.SelectMany(testWriter => testWriter.Tests).ToList();
        for (var i = 0; i < tests.Count; i++)
        {
            var test = tests[i];
            Append(test);

            if (i != tests.Count - 1)
                AppendLine();
        }
    }
}