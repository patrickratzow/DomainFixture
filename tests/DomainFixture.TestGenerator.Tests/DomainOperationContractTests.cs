using System.Collections.Generic;
using System;
using DomainFixture.Contracts;
using FluentAssertions;
using NUnit.Framework;

namespace DomainFixture.TestGenerator.Tests;

[TestFixture]
public sealed class DomainOperationContractTests
{
    [Test]
    public void Constructor_ShouldCopyOrderedParameterBindings()
    {
        var parameters = new List<DomainOperationParameterContract>
        {
            new("name", "System.String", "Name"),
            new("realm", "System.String", "Realm")
        };

        var contract = new DomainOperationContract(
            DomainOperationKinds.StaticFactory,
            "Example.QualifiedName",
            "Example.QualifiedName",
            "From",
            parameters);

        parameters.Clear();

        contract.SchemaVersion.Should().Be(DomainOperationContract.CurrentSchemaVersion);
        contract.OperationId.Should().Be(
            "domainfixture.operation.static-factory:Example.QualifiedName.From(System.String,System.String)");
        contract.ReturnTypeName.Should().Be("Example.QualifiedName");
        contract.Parameters.Should().HaveCount(2);
        contract.Parameters[0].MemberPath.Should().Be("Name");
        contract.Parameters[1].MemberPath.Should().Be("Realm");
    }

    [Test]
    public void ExplicitIdentityAndReturnType_ShouldBePreserved()
    {
        var contract = new DomainOperationContract(
            "registration.approve",
            DomainOperationKinds.InstanceCommand,
            "Example.Registration",
            "Example.Registration",
            "Approve",
            "System.Void",
            Array.Empty<DomainOperationParameterContract>());

        contract.OperationId.Should().Be("registration.approve");
        contract.ReturnTypeName.Should().Be("System.Void");
        contract.Parameters.Should().BeEmpty();
    }

    [Test]
    public void InstanceCommand_ShouldPreserveParameterizedCommandBindings()
    {
        var contract = new DomainOperationContract(
            "registration.approve",
            DomainOperationKinds.InstanceCommand,
            "Example.Registration",
            "Example.Registration",
            "Approve",
            "System.Void",
            new[] { new DomainOperationParameterContract("reason", "System.String", "Reason") });

        contract.Parameters.Should().ContainSingle();
        contract.Parameters[0].Name.Should().Be("reason");
    }

    [Test]
    public void ConstructionOperation_ShouldStillRequireParameters()
    {
        var action = () => new DomainOperationContract(
            DomainOperationKinds.StaticFactory,
            "Example.Username",
            "Example.Username",
            "From",
            Array.Empty<DomainOperationParameterContract>());

        action.Should().Throw<ArgumentException>()
            .WithParameterName("parameters");
    }
}
