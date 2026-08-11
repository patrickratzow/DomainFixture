using System.Collections.Generic;
using DomainFixture.Contracts;
using FluentAssertions;
using NUnit.Framework;

namespace DomainFixture.TestGenerator.Tests;

[TestFixture]
public sealed class DomainConstraintContractTests
{
    [Test]
    public void Constructor_ShouldCopyParametersAndParseInvariantInt32Values()
    {
        var parameters = new Dictionary<string, string>
        {
            [DomainConstraintParameters.Minimum] = "4"
        };
        var contract = new DomainConstraintContract(
            DomainConstraintKinds.TextMinimumLength,
            "Example.Adapter",
            "Example.Subject",
            "Name",
            parameters);

        parameters[DomainConstraintParameters.Minimum] = "99";

        contract.TryGetInt32(DomainConstraintParameters.Minimum, out var minimum).Should().BeTrue();
        minimum.Should().Be(4);
        contract.SchemaVersion.Should().Be(DomainConstraintContract.CurrentSchemaVersion);
    }
}
