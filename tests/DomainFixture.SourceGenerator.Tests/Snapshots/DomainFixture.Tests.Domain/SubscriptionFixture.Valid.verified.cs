using System.CodeDom.Compiler;
using NUnit.Framework;
using System;
using Subscription = global::DomainFixture.Tests.Domain.Entities.Subscription;
using SubscriptionStatus = global::DomainFixture.Tests.Domain.Entities.SubscriptionStatus;

namespace DomainFixture.Tests.Generation
{
    [GeneratedCode("DomainFixture.TestGenerator", "1.0.0.0")]
    [TestFixture]
    public sealed class SubscriptionValidGeneratedTests
    {
        [Test]
        public void Valid_Construction_Create_PlanName_Seats_Id_BaselineSucceeds()
        {
            Subscription baseline = SubscriptionFixtureFactory.Valid.Create();
            Subscription constructed = Subscription.Create(baseline.PlanName, baseline.Seats, baseline.Id);
            Assert.That(constructed, Is.Not.Null);
        }

        [Test]
        public void Valid_Construction_Create_PlanName_Seats_Id_ArgumentsRoundTrip()
        {
            Subscription baseline = SubscriptionFixtureFactory.Valid.Create();
            Subscription constructed = Subscription.Create(baseline.PlanName, baseline.Seats, baseline.Id);
            Assert.That(constructed.PlanName, Is.EqualTo(baseline.PlanName));
            Assert.That(constructed.Seats, Is.EqualTo(baseline.Seats));
            Assert.That(constructed.Id, Is.EqualTo(baseline.Id));
        }

        [Test]
        public void Valid_Transition_Can_reserve_ReachesExpectedState()
        {
            Subscription subject = SubscriptionFixtureFactory.Valid.Create();
            var result = subject.CanReserve(0);
            Func<bool, bool> predicate = result => result;
            Assert.That(predicate(result), Is.True);
        }

        [Test]
        public void Valid_Transition_Activate_ReachesExpectedState()
        {
            Subscription subject = SubscriptionFixtureFactory.Valid.Create();
            subject = subject.Activate(0);
            Assert.That(subject.Status, Is.EqualTo(SubscriptionStatus.Active));
        }

        [Test]
        public void Valid_Transition_Activate_PreservesIdentity()
        {
            Subscription subject = SubscriptionFixtureFactory.Valid.Create();
            var identity = subject.Id;
            subject = subject.Activate(0);
            Assert.That(subject.Id, Is.EqualTo(identity));
        }

        [Test]
        public void Valid_State_Initially_pending_Matches()
        {
            Subscription subject = SubscriptionFixtureFactory.Valid.Create();
            Assert.That(subject.Details.Status, Is.EqualTo(SubscriptionStatus.Pending));
        }
    }
}
