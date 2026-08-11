using System.CodeDom.Compiler;
using NUnit.Framework;

namespace DomainFixture.Tests.Generation
{
    [GeneratedCode("DomainFixture.TestGenerator", "1.0.0.0")]
    [TestFixture]
    public sealed class RegistrationRequestRegistrationGeneratedTests
    {
        [Test]
        public void Registration_Baseline_IsValid()
        {
            global::DomainFixture.Tests.Domain.Entities.RegistrationRequest subject = global::DomainFixture.Tests.Generation.RegistrationRequestFixture.Baseline();
            var validator = global::DomainFixture.Tests.Generation.RegistrationRequestFixture.Validator();
            var report = validator.Validate(subject);
            Assert.That(report.IsValid, Is.True);
        }

        [Test]
        public void Registration_Description_LengthBelowMinimum_IsInvalid()
        {
            global::DomainFixture.Tests.Domain.Entities.RegistrationRequest subject = global::DomainFixture.Tests.Generation.RegistrationRequestFixture.Baseline();
            subject.Description = new string ('a', 3);
            var validator = global::DomainFixture.Tests.Generation.RegistrationRequestFixture.Validator();
            var report = validator.Validate(subject);
            Assert.That(report.ContainsFailure("Description", "DESCRIPTION_LENGTH"), Is.True);
        }

        [Test]
        public void Registration_Description_LengthAtMinimum_IsValid()
        {
            global::DomainFixture.Tests.Domain.Entities.RegistrationRequest subject = global::DomainFixture.Tests.Generation.RegistrationRequestFixture.Baseline();
            subject.Description = new string ('a', 4);
            var validator = global::DomainFixture.Tests.Generation.RegistrationRequestFixture.Validator();
            var report = validator.Validate(subject);
            Assert.That(report.IsValid, Is.True);
        }

        [Test]
        public void Registration_Description_LengthAtMaximum_IsValid()
        {
            global::DomainFixture.Tests.Domain.Entities.RegistrationRequest subject = global::DomainFixture.Tests.Generation.RegistrationRequestFixture.Baseline();
            subject.Description = new string ('a', 8);
            var validator = global::DomainFixture.Tests.Generation.RegistrationRequestFixture.Validator();
            var report = validator.Validate(subject);
            Assert.That(report.IsValid, Is.True);
        }

        [Test]
        public void Registration_Description_LengthAboveMaximum_IsInvalid()
        {
            global::DomainFixture.Tests.Domain.Entities.RegistrationRequest subject = global::DomainFixture.Tests.Generation.RegistrationRequestFixture.Baseline();
            subject.Description = new string ('a', 9);
            var validator = global::DomainFixture.Tests.Generation.RegistrationRequestFixture.Validator();
            var report = validator.Validate(subject);
            Assert.That(report.ContainsFailure("Description", "DESCRIPTION_LENGTH"), Is.True);
        }

        [Test]
        public void Registration_Description_NotEmpty_Empty_IsInvalid()
        {
            global::DomainFixture.Tests.Domain.Entities.RegistrationRequest subject = global::DomainFixture.Tests.Generation.RegistrationRequestFixture.Baseline();
            subject.Description = "";
            var validator = global::DomainFixture.Tests.Generation.RegistrationRequestFixture.Validator();
            var report = validator.Validate(subject);
            Assert.That(report.ContainsFailure("Description", "DESCRIPTION_NOT_EMPTY"), Is.True);
        }

        [Test]
        public void Registration_Description_NotNull_Null_IsInvalid()
        {
            global::DomainFixture.Tests.Domain.Entities.RegistrationRequest subject = global::DomainFixture.Tests.Generation.RegistrationRequestFixture.Baseline();
            subject.Description = null;
            var validator = global::DomainFixture.Tests.Generation.RegistrationRequestFixture.Validator();
            var report = validator.Validate(subject);
            Assert.That(report.ContainsFailure("Description", "DESCRIPTION_REQUIRED"), Is.True);
        }
    }
}
