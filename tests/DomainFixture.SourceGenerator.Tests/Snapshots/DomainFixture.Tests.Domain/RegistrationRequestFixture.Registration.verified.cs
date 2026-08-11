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
            var validator = new global::DomainFixture.Tests.Generation.RegistrationRequestRegistrationFluentValidationAdapter();
            var report = validator.Validate(subject);
            Assert.That(report.IsValid, Is.True);
        }

        [Test]
        public void Registration_Description_LengthBelowMinimum_IsInvalid()
        {
            global::DomainFixture.Tests.Domain.Entities.RegistrationRequest subject = global::DomainFixture.Tests.Generation.RegistrationRequestFixture.Baseline();
            subject.Description = new string ('a', 3);
            var validator = new global::DomainFixture.Tests.Generation.RegistrationRequestRegistrationFluentValidationAdapter();
            var report = validator.Validate(subject);
            Assert.That(report.ContainsFailure("Description", "DESCRIPTION_LENGTH"), Is.True);
        }

        [Test]
        public void Registration_Description_LengthAtMinimum_IsValid()
        {
            global::DomainFixture.Tests.Domain.Entities.RegistrationRequest subject = global::DomainFixture.Tests.Generation.RegistrationRequestFixture.Baseline();
            subject.Description = new string ('a', 4);
            var validator = new global::DomainFixture.Tests.Generation.RegistrationRequestRegistrationFluentValidationAdapter();
            var report = validator.Validate(subject);
            Assert.That(report.IsValid, Is.True);
        }

        [Test]
        public void Registration_Description_LengthAtMaximum_IsValid()
        {
            global::DomainFixture.Tests.Domain.Entities.RegistrationRequest subject = global::DomainFixture.Tests.Generation.RegistrationRequestFixture.Baseline();
            subject.Description = new string ('a', 8);
            var validator = new global::DomainFixture.Tests.Generation.RegistrationRequestRegistrationFluentValidationAdapter();
            var report = validator.Validate(subject);
            Assert.That(report.IsValid, Is.True);
        }

        [Test]
        public void Registration_Description_LengthAboveMaximum_IsInvalid()
        {
            global::DomainFixture.Tests.Domain.Entities.RegistrationRequest subject = global::DomainFixture.Tests.Generation.RegistrationRequestFixture.Baseline();
            subject.Description = new string ('a', 9);
            var validator = new global::DomainFixture.Tests.Generation.RegistrationRequestRegistrationFluentValidationAdapter();
            var report = validator.Validate(subject);
            Assert.That(report.ContainsFailure("Description", "DESCRIPTION_LENGTH"), Is.True);
        }

        [Test]
        public void Registration_Description_NotEmpty_Empty_IsInvalid()
        {
            global::DomainFixture.Tests.Domain.Entities.RegistrationRequest subject = global::DomainFixture.Tests.Generation.RegistrationRequestFixture.Baseline();
            subject.Description = "";
            var validator = new global::DomainFixture.Tests.Generation.RegistrationRequestRegistrationFluentValidationAdapter();
            var report = validator.Validate(subject);
            Assert.That(report.ContainsFailure("Description", "DESCRIPTION_NOT_EMPTY"), Is.True);
        }

        [Test]
        public void Registration_Description_NotNull_Null_IsInvalid()
        {
            global::DomainFixture.Tests.Domain.Entities.RegistrationRequest subject = global::DomainFixture.Tests.Generation.RegistrationRequestFixture.Baseline();
            subject.Description = null;
            var validator = new global::DomainFixture.Tests.Generation.RegistrationRequestRegistrationFluentValidationAdapter();
            var report = validator.Validate(subject);
            Assert.That(report.ContainsFailure("Description", "DESCRIPTION_REQUIRED"), Is.True);
        }
    }
}

namespace DomainFixture.Tests.Generation
{
    internal sealed class RegistrationRequestRegistrationFluentValidationAdapter : global::DomainFixture.Validation.IFixtureValidator<global::DomainFixture.Tests.Domain.Entities.RegistrationRequest>
    {
        private readonly global::DomainFixture.Tests.Domain.Entities.RegistrationRequestValidator _validator = new global::DomainFixture.Tests.Domain.Entities.RegistrationRequestValidator();

        public global::DomainFixture.Validation.ValidationReport Validate(global::DomainFixture.Tests.Domain.Entities.RegistrationRequest subject)
        {
            var context = new global::FluentValidation.ValidationContext<global::DomainFixture.Tests.Domain.Entities.RegistrationRequest>(subject);
            var result = _validator.Validate(context);
            var failures = new global::System.Collections.Generic.List<global::DomainFixture.Validation.ValidationFailure>();
            foreach (var error in result.Errors)
            {
                failures.Add(new global::DomainFixture.Validation.ValidationFailure(error.PropertyName, error.ErrorCode, error.ErrorMessage));
            }

            return new global::DomainFixture.Validation.ValidationReport(failures);
        }
    }
}
