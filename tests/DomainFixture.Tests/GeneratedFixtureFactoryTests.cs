using System;
using DomainFixture.Tests.Domain.Entities;
using DomainFixture.Tests.Domain.ValueObjects;
using DomainFixture.Tests.Generation;
using FluentAssertions;
using NUnit.Framework;

namespace DomainFixture.Tests;

[TestFixture]
public sealed class GeneratedFixtureFactoryTests
{
    [Test]
    public void UsernameFactory_ShouldCreateFreshValidInstances()
    {
        var first = UsernameFixtureFactory.Validation.Create();
        var second = UsernameFixtureFactory.Validation.Create();

        first.Should().NotBeSameAs(second);
        first.Value.Should().Be("baseline");
        second.Value.Should().Be("baseline");
    }

    [Test]
    public void QualifiedHandleFactory_TransformShouldNotAffectLaterCreations()
    {
        var transformed = QualifiedHandleFixtureFactory.Validation.Create(
            value => value with { Realm = "internal" });
        var subsequent = QualifiedHandleFixtureFactory.Validation.Create();

        transformed.Realm.Should().Be("internal");
        transformed.Name.Should().Be("alice");
        subsequent.Realm.Should().Be("example");
        subsequent.Should().NotBeSameAs(transformed);
    }

    [Test]
    public void RegistrationFactories_ShouldCreateFreshIsolatedScenarioInstances()
    {
        var pending = RegistrationFixtureFactory.Pending.Create();
        var anotherPending = RegistrationFixtureFactory.Pending.Create();
        var approved = RegistrationFixtureFactory.Approved.Create();

        pending.Should().NotBeSameAs(anotherPending);
        pending.Id.Should().Be(anotherPending.Id);
        pending.Status.Should().Be(RegistrationStatus.Pending);
        approved.Status.Should().Be(RegistrationStatus.Approved);

        pending.Approve();

        pending.Status.Should().Be(RegistrationStatus.Approved);
        RegistrationFixtureFactory.Pending.Create().Status.Should().Be(RegistrationStatus.Pending);
    }

    [Test]
    public void SynthesizedSubscriptionFactory_ShouldBeUsableFromHandwrittenTests()
    {
        var subject = SubscriptionFixtureFactory.Valid.Create();

        subject.PlanName.Should().Be("a");
        subject.Seats.Should().Be(0);
        subject.Id.Should().NotBe(Guid.Empty);
        subject.Status.Should().Be(SubscriptionStatus.Pending);
        subject.Details.Status.Should().Be(SubscriptionStatus.Pending);
    }

    [Test]
    public void GenericResultFactory_ShouldExposeTheUnwrappedValidSubject()
    {
        var subject = ResultDisplayNameFixtureFactory.Valid.Create();

        subject.Value.Should().Be("a");
        ResultDisplayName.Create(string.Empty).IsSuccess.Should().BeFalse();
    }

    [Test]
    public void NestedSubscriptionFactory_ShouldComposeConfiguredObjectGraph()
    {
        var subject = TenantSubscriptionFixtureFactory.Valid.Create();

        subject.Owner.Value.Should().Be("baseline");
        subject.Participants.Should().ContainSingle()
            .Which.Value.Should().Be("baseline");
        subject.ParticipantsByRole.Should().ContainKey("a")
            .WhoseValue.Value.Should().Be("baseline");
        subject.BillingCycle.Should().Be(BillingCycle.Monthly);
    }
}
