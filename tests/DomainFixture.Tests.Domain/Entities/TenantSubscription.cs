using System.Collections.Generic;
using DomainFixture.Tests.Domain.ValueObjects;

namespace DomainFixture.Tests.Domain.Entities;

public enum BillingCycle
{
    Unknown,
    Monthly
}

public sealed class TenantSubscription
{
    private TenantSubscription(
        Username owner,
        IReadOnlyList<Username> participants,
        IReadOnlyDictionary<string, Username> participantsByRole,
        BillingCycle billingCycle)
    {
        Owner = owner;
        Participants = participants;
        ParticipantsByRole = participantsByRole;
        BillingCycle = billingCycle;
    }

    public Username Owner { get; }
    public IReadOnlyList<Username> Participants { get; }
    public IReadOnlyDictionary<string, Username> ParticipantsByRole { get; }
    public BillingCycle BillingCycle { get; }

    public static TenantSubscription Create(
        Username owner,
        IReadOnlyList<Username> participants,
        IReadOnlyDictionary<string, Username> participantsByRole,
        BillingCycle billingCycle) =>
        new(owner, participants, participantsByRole, billingCycle);
}
