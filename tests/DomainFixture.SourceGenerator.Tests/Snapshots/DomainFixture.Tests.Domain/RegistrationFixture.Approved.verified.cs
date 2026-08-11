using System.CodeDom.Compiler;
using NUnit.Framework;
using System;
using Registration = global::DomainFixture.Tests.Domain.Entities.Registration;

namespace DomainFixture.Tests.Generation
{
    [GeneratedCode("DomainFixture.TestGenerator", "1.0.0.0")]
    [TestFixture]
    public sealed class RegistrationApprovedGeneratedTests
    {
        [Test]
        public void Approved_Construction_Constructor_Id_Status_BaselineSucceeds()
        {
            Registration baseline = RegistrationFixture.Approved();
            Registration constructed = new Registration(baseline.Id, baseline.Status);
            Assert.That(constructed, Is.Not.Null);
        }

        [Test]
        public void Approved_Construction_Constructor_Id_Status_ArgumentsRoundTrip()
        {
            Registration baseline = RegistrationFixture.Approved();
            Registration constructed = new Registration(baseline.Id, baseline.Status);
            Assert.That(constructed.Id, Is.EqualTo(baseline.Id));
            Assert.That(constructed.Status, Is.EqualTo(baseline.Status));
        }

        [Test]
        public void Approved_Transition_ApproveAgain_IsRejected()
        {
            Registration subject = RegistrationFixture.Approved();
            Assert.Throws<InvalidOperationException>(() => subject.Approve());
        }
    }
}
