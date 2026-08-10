using DomainFixture.TestGenerator.Framework;
using FluentAssertions;
using NUnit.Framework;

namespace DomainFixture.TestGenerator.Tests;

[TestFixture]
public class FileCodeWriterTests
{
    private record User(string Name, int Age);
    private FileCodeWriter<User> _writer = null!;

    [SetUp]
    public void SetUp()
    {
        _writer = new("Test", TestingFramework.NUnit);
    }

    [Test]
    public void ToString_ShouldPrintEmptyClass_WhenNoTestWritersHaveBeenAdded()
    {
        var version = typeof(FileCodeWriter<>).Assembly.GetName().Version!.ToString();
        var result = _writer.ToString();

        var expected = $@"using System.CodeDom.Compiler;
using DomainFixture;
using NUnit.Framework;

namespace Test
{{
    [GeneratedCode(""DomainFixture.TestGenerator"", ""{version}"")]
    [TestFixture]
    public class UserTests
    {{
    }}
}}
";

        result.Replace("\r\n", "\n").Should().Be(expected.Replace("\r\n", "\n"));
    }

    [Test]
    public void ToString_ShouldBeIdempotent()
    {
        var first = _writer.ToString();
        var second = _writer.ToString();

        second.Should().Be(first);
    }
}
