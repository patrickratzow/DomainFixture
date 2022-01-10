using DomainFixture.TestGenerator.Framework;
using FluentAssertions;
using NUnit.Framework;

namespace DomainFixture.TestGenerator.Tests;

[TestFixture]
public class FileCodeWriterTests
{
    private record User(string Name, int Age);
    private FileCodeWriter<User> _writer;

    [SetUp]
    public void SetUp()
    {
        _writer = new("Test", TestingFramework.NUnit);
    }

    [Test]
    public void ToString_ShouldPrintEmptyClass_WhenNoTestWritersHaveBeenAdded()
    {
        // Arrange
        var version = typeof(FileCodeWriter<>).Assembly.GetName().Version!.ToString();
        
        // Act
        var result = _writer.ToString();
        
        // Assert
        result.Should().Be(
$@"using System.CodeDom.Compiler;
using DomainFixture;

namespace Test
{{
    [GeneratedCode(""DomainFixture.TestGenerator"", ""{version}"")]
    [TestFixture]
    public class UserTests
    {{
    }}
}}
");
    }
}