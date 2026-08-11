using System.CodeDom.Compiler;
using NUnit.Framework;
using System.Collections.Generic;
using IFixtureValidator = global::DomainFixture.Validation.IFixtureValidator<global::DomainFixture.Tests.Domain.Entities.RegistrationRequest>;
using RegistrationRequest = global::DomainFixture.Tests.Domain.Entities.RegistrationRequest;
using RegistrationRequestValidator = global::DomainFixture.Tests.Domain.Entities.RegistrationRequestValidator;
using ValidationContext = global::FluentValidation.ValidationContext<global::DomainFixture.Tests.Domain.Entities.RegistrationRequest>;
using ValidationFailure = global::DomainFixture.Validation.ValidationFailure;
using ValidationReport = global::DomainFixture.Validation.ValidationReport;

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
            var validator = new RegistrationRequestRegistrationFluentValidationAdapter();
            var report = validator.Validate(subject);
            Assert.That(report.IsValid, Is.True);
        }

        [Test]
        public void Registration_Description_LengthBelowMinimum_IsInvalid()
        {
            RegistrationRequest subject = RegistrationRequestFixture.Baseline();
            subject.Description = new string ('a', 3);
            var validator = new RegistrationRequestRegistrationFluentValidationAdapter();
            var report = validator.Validate(subject);
            Assert.That(report.ContainsFailure("Description", "DESCRIPTION_LENGTH"), Is.True);
        }

        [Test]
        public void Registration_Description_LengthAtMinimum_IsValid()
        {
            RegistrationRequest subject = RegistrationRequestFixture.Baseline();
            subject.Description = new string ('a', 4);
            var validator = new RegistrationRequestRegistrationFluentValidationAdapter();
            var report = validator.Validate(subject);
            Assert.That(report.IsValid, Is.True);
        }

        [Test]
        public void Registration_Description_LengthAtMaximum_IsValid()
        {
            RegistrationRequest subject = RegistrationRequestFixture.Baseline();
            subject.Description = new string ('a', 8);
            var validator = new RegistrationRequestRegistrationFluentValidationAdapter();
            var report = validator.Validate(subject);
            Assert.That(report.IsValid, Is.True);
        }

        [Test]
        public void Registration_Description_LengthAboveMaximum_IsInvalid()
        {
            RegistrationRequest subject = RegistrationRequestFixture.Baseline();
            subject.Description = new string ('a', 9);
            var validator = new RegistrationRequestRegistrationFluentValidationAdapter();
            var report = validator.Validate(subject);
            Assert.That(report.ContainsFailure("Description", "DESCRIPTION_LENGTH"), Is.True);
        }

        [Test]
        public void Registration_Description_NotEmpty_Empty_IsInvalid()
        {
            RegistrationRequest subject = RegistrationRequestFixture.Baseline();
            subject.Description = "";
            var validator = new RegistrationRequestRegistrationFluentValidationAdapter();
            var report = validator.Validate(subject);
            Assert.That(report.ContainsFailure("Description", "DESCRIPTION_NOT_EMPTY"), Is.True);
        }

        [Test]
        public void Registration_Description_NotNull_Null_IsInvalid()
        {
            RegistrationRequest subject = RegistrationRequestFixture.Baseline();
            subject.Description = null;
            var validator = new RegistrationRequestRegistrationFluentValidationAdapter();
            var report = validator.Validate(subject);
            Assert.That(report.ContainsFailure("Description", "DESCRIPTION_REQUIRED"), Is.True);
        }
    }
}

namespace DomainFixture.Tests.Generation
{
    internal sealed class RegistrationRequestRegistrationFluentValidationAdapter : IFixtureValidator
    {
        private readonly RegistrationRequestValidator _validator = new RegistrationRequestValidator();
        public ValidationReport Validate(RegistrationRequest subject)
        {
            var context = new ValidationContext(subject);
            var result = _validator.Validate(context);
            var failures = new List<ValidationFailure>();
            foreach (var error in result.Errors)
            {
                failures.Add(new ValidationFailure(error.PropertyName, error.ErrorCode, error.ErrorMessage));
            }

            return new ValidationReport(failures);
        }
    }
}
