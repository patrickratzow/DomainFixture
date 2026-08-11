using System.CodeDom.Compiler;
using NUnit.Framework;
using Registration = global::DomainFixture.Tests.Domain.Entities.Registration;
using RegistrationStatus = global::DomainFixture.Tests.Domain.Entities.RegistrationStatus;

namespace DomainFixture.Tests.Generation
{
    [GeneratedCode("DomainFixture.TestGenerator", "1.0.0.0")]
    [TestFixture]
    public sealed class RegistrationPendingGeneratedTests
    {
        [Test]
        public void Pending_Construction_Constructor_Id_Status_BaselineSucceeds()
        {
            Registration baseline = RegistrationFixture.Pending();
            Registration constructed = new Registration(baseline.Id, baseline.Status);
            Assert.That(constructed, Is.Not.Null);
        }

        [Test]
        public void Pending_Construction_Constructor_Id_Status_ArgumentsRoundTrip()
        {
            Registration baseline = RegistrationFixture.Pending();
            Registration constructed = new Registration(baseline.Id, baseline.Status);
            Assert.That(constructed.Id, Is.EqualTo(baseline.Id));
            Assert.That(constructed.Status, Is.EqualTo(baseline.Status));
        }

        [Test]
        public void Pending_Transition_Approve_ReachesExpectedState()
        {
            Registration subject = RegistrationFixture.Pending();
            subject.Approve();
            Assert.That(subject.Status, Is.EqualTo(RegistrationStatus.Approved));
        }

        [Test]
        public void Pending_Transition_Approve_PreservesIdentity()
        {
            Registration subject = RegistrationFixture.Pending();
            var identity = subject.Id;
            subject.Approve();
            Assert.That(subject.Id, Is.EqualTo(identity));
        }
    }
}
