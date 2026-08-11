using System;
using DomainFixture.Generation;
using DomainFixture.Tests.Domain.Entities;

namespace DomainFixture.Tests.Generation;

public sealed class RegistrationFixture : IFixtureTestConfiguration<Registration>
{
    private static readonly Guid PendingId = new("6b8e1fc1-c98d-4fd3-93e9-e734fd06e387");
    private static readonly Guid ApprovedId = new("be132e0a-4015-49f5-aa35-f2e123bb9cf9");

    public void Configure(IFixtureTestBuilder<Registration> fixture)
    {
        fixture.Recipe("Pending")
            .Baseline(Pending)
            .Transition(
                "Approve",
                subject => subject.Approve(),
                subject => subject.Status,
                RegistrationStatus.Approved);

        fixture.Recipe("Approved")
            .Baseline(Approved)
            .RejectTransition<InvalidOperationException>(
                "ApproveAgain",
                subject => subject.Approve());
    }

    public static Registration Pending() =>
        new(PendingId, RegistrationStatus.Pending);

    public static Registration Approved() =>
        new(ApprovedId, RegistrationStatus.Approved);
}
