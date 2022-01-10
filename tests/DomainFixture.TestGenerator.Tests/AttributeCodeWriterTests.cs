using System.Collections.Generic;
using DomainFixture.TestGenerator.Framework;
using FluentAssertions;
using NUnit.Framework;
using Environment = System.Environment;

namespace DomainFixture.TestGenerator.Tests;

[TestFixture]
public class AttributeCodeWriterTests
{
    private AttributeCodeWriter _writer = null!;

    [SetUp]
    public void SetUp()
    {
        _writer = new();
    }
    
    [Test]
    public void ToString_ShouldWriteAttribute_WhenGivenOneAttribute()
    {
        // Arrange
        var attributes = new List<Attribute>
        {
            new("Test", "TestNamespace")
        };
        _writer.AddAttributes(attributes);

        // Act
        var result = _writer.ToString();

        // Assert
        result.Should().Be("[Test]");
    }
    
    [Test]
    public void ToString_ShouldWriteAttributeOnMultipleLines_WhenGivenMoreThanOneAttribute()
    {
        // Arrange
        var attributes = new List<Attribute>
        {
            new("Test", "TestNamespace"),
            new("Category", "TestNamespace", new() { "\"User\"" })
        };
        _writer.AddAttributes(attributes);

        // Act
        var result = _writer.ToString();

        // Assert
        result.Should().Be($"[Test]{Environment.NewLine}[Category(\"User\")]");
    }
}