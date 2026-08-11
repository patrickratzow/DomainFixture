using System.CodeDom.Compiler;
using NUnit.Framework;
using RegistrationRequest = global::DomainFixture.Tests.Domain.Entities.RegistrationRequest;
using RegistrationRequestRegistrationRequestValidatorAdapter_f69d553f8aff41e3 = global::DomainFixture.Modules.FluentValidation.Generated.RegistrationRequestRegistrationRequestValidatorAdapter_f69d553f8aff41e3;

namespace DomainFixture.Tests.Generation
{
    [GeneratedCode("DomainFixture.TestGenerator", "1.0.0.0")]
    [TestFixture]
    public sealed class RegistrationRequestRegistrationGeneratedTests
    {
        [Test]
        public void Registration_Baseline_IsValid()
        {
            RegistrationRequest subject = RegistrationRequestFixture.Baseline();
            var validator = new RegistrationRequestRegistrationRequestValidatorAdapter_f69d553f8aff41e3();
            var report = validator.Validate(subject);
            Assert.That(report.IsValid, Is.True);
        }

        [Test]
        public void Registration_Description_LengthBelowMinimum_IsInvalid()
        {
            RegistrationRequest subject = RegistrationRequestFixture.Baseline();
            subject.Description = new string ('a', 3);
            var validator = new RegistrationRequestRegistrationRequestValidatorAdapter_f69d553f8aff41e3();
            var report = validator.Validate(subject);
            Assert.That(report.ContainsFailure("Description", "DESCRIPTION_LENGTH"), Is.True);
        }

        [Test]
        public void Registration_Description_LengthAtMinimum_IsValid()
        {
            RegistrationRequest subject = RegistrationRequestFixture.Baseline();
            subject.Description = new string ('a', 4);
            var validator = new RegistrationRequestRegistrationRequestValidatorAdapter_f69d553f8aff41e3();
            var report = validator.Validate(subject);
            Assert.That(report.IsValid, Is.True);
        }

        [Test]
        public void Registration_Description_LengthAtMaximum_IsValid()
        {
            RegistrationRequest subject = RegistrationRequestFixture.Baseline();
            subject.Description = new string ('a', 8);
            var validator = new RegistrationRequestRegistrationRequestValidatorAdapter_f69d553f8aff41e3();
            var report = validator.Validate(subject);
            Assert.That(report.IsValid, Is.True);
        }

        [Test]
        public void Registration_Description_LengthAboveMaximum_IsInvalid()
        {
            RegistrationRequest subject = RegistrationRequestFixture.Baseline();
            subject.Description = new string ('a', 9);
            var validator = new RegistrationRequestRegistrationRequestValidatorAdapter_f69d553f8aff41e3();
            var report = validator.Validate(subject);
            Assert.That(report.ContainsFailure("Description", "DESCRIPTION_LENGTH"), Is.True);
        }

        [Test]
        public void Registration_Description_NotEmpty_Empty_IsInvalid()
        {
            RegistrationRequest subject = RegistrationRequestFixture.Baseline();
            subject.Description = "";
            var validator = new RegistrationRequestRegistrationRequestValidatorAdapter_f69d553f8aff41e3();
            var report = validator.Validate(subject);
            Assert.That(report.ContainsFailure("Description", "DESCRIPTION_NOT_EMPTY"), Is.True);
        }

        [Test]
        public void Registration_Description_NotNull_Null_IsInvalid()
        {
            RegistrationRequest subject = RegistrationRequestFixture.Baseline();
            subject.Description = null;
            var validator = new RegistrationRequestRegistrationRequestValidatorAdapter_f69d553f8aff41e3();
            var report = validator.Validate(subject);
            Assert.That(report.ContainsFailure("Description", "DESCRIPTION_REQUIRED"), Is.True);
        }
    }
}
