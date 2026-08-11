using System.CodeDom.Compiler;
using NUnit.Framework;
using TenantSubscription = global::DomainFixture.Tests.Domain.Entities.TenantSubscription;

namespace DomainFixture.Tests.Generation
{
    [GeneratedCode("DomainFixture.TestGenerator", "1.0.0.0")]
    [TestFixture]
    public sealed class TenantSubscriptionValidGeneratedTests
    {
        [Test]
        public void Valid_Construction_Create_Owner_Participants_ParticipantsByRole_BillingCycle_BaselineSucceeds()
        {
            TenantSubscription baseline = TenantSubscriptionFixtureFactory.Valid.Create();
            TenantSubscription constructed = TenantSubscription.Create(baseline.Owner, baseline.Participants, baseline.ParticipantsByRole, baseline.BillingCycle);
            Assert.That(constructed, Is.Not.Null);
        }

        [Test]
        public void Valid_Construction_Create_Owner_Participants_ParticipantsByRole_BillingCycle_ArgumentsRoundTrip()
        {
            TenantSubscription baseline = TenantSubscriptionFixtureFactory.Valid.Create();
            TenantSubscription constructed = TenantSubscription.Create(baseline.Owner, baseline.Participants, baseline.ParticipantsByRole, baseline.BillingCycle);
            Assert.That(constructed.Owner, Is.EqualTo(baseline.Owner));
            Assert.That(constructed.Participants, Is.EqualTo(baseline.Participants));
            Assert.That(constructed.ParticipantsByRole, Is.EqualTo(baseline.ParticipantsByRole));
            Assert.That(constructed.BillingCycle, Is.EqualTo(baseline.BillingCycle));
        }
    }
}
