using System;
using System.Collections.Generic;
using System.Linq;
using DomainFixture.Contracts;
using FluentAssertions;
using NUnit.Framework;

namespace DomainFixture.TestGenerator.Tests;

[TestFixture]
public sealed class DomainOperationOutcomeContractTests
{
    [Test]
    public void ExceptionOutcome_ShouldCopyItsExceptionType()
    {
        var parameters = new List<string> { "Example.DomainException" };

        var contract = new DomainOperationOutcomeContract(
            "username.from",
            DomainOperationOutcomeKinds.ThrowsException,
            parameters);

        parameters.Clear();

        contract.SchemaVersion.Should().Be(DomainOperationOutcomeContract.CurrentSchemaVersion);
        contract.OperationId.Should().Be("username.from");
        contract.Parameters.Should().Equal("Example.DomainException");
    }

    [TestCase(0)]
    [TestCase(2)]
    public void ExceptionOutcome_ShouldRequireExactlyOneExceptionType(int parameterCount)
    {
        var parameters = new string[parameterCount];
        for (var index = 0; index < parameters.Length; index++)
            parameters[index] = $"Example.Exception{index}";

        var action = () => new DomainOperationOutcomeContract(
            "username.from",
            DomainOperationOutcomeKinds.ThrowsException,
            parameters);

        action.Should().Throw<ArgumentException>()
            .WithParameterName("parameters");
    }

    [Test]
    public void ResultOutcome_ShouldCopySuccessAndValuePaths()
    {
        var contract = new DomainOperationOutcomeContract(
            "name.create",
            DomainOperationOutcomeKinds.ReturnsResult,
            new[] { "IsSuccess", "Value" });

        contract.Parameters.Should().Equal("IsSuccess", "Value");
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(3)]
    public void ResultOutcome_ShouldRequireSuccessAndValuePaths(int parameterCount)
    {
        var action = () => new DomainOperationOutcomeContract(
            "name.create",
            DomainOperationOutcomeKinds.ReturnsResult,
            Enumerable.Range(0, parameterCount).Select(index => $"Path{index}").ToArray());

        action.Should().Throw<ArgumentException>()
            .WithParameterName("parameters");
    }
}
