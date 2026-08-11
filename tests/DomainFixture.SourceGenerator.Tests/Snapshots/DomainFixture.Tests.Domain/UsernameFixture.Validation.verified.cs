using System.CodeDom.Compiler;
using NUnit.Framework;

namespace DomainFixture.Tests.Generation
{
    [GeneratedCode("DomainFixture.TestGenerator", "1.0.0.0")]
    [TestFixture]
    public sealed class UsernameValidationGeneratedTests
    {
        [Test]
        public void Validation_Baseline_IsValid()
        {
            global::DomainFixture.Tests.Domain.ValueObjects.Username subject = global::DomainFixture.Tests.Generation.UsernameFixture.Baseline();
            var validator = new global::DomainFixture.Tests.Generation.UsernameValidationFluentValidationAdapter();
            var report = validator.Validate(subject);
            Assert.That(report.IsValid, Is.True);
        }

        [Test]
        public void Validation_Value_LengthAtMaximum_IsValid()
        {
            global::DomainFixture.Tests.Domain.ValueObjects.Username subject = global::DomainFixture.Tests.Generation.UsernameFixture.Baseline();
            subject = global::DomainFixture.Tests.Generation.UsernameTestFactory.WithValue(subject, new string ('a', 128));
            var validator = new global::DomainFixture.Tests.Generation.UsernameValidationFluentValidationAdapter();
            var report = validator.Validate(subject);
            Assert.That(report.IsValid, Is.True);
        }

        [Test]
        public void Validation_Value_LengthAboveMaximum_IsInvalid()
        {
            global::DomainFixture.Tests.Domain.ValueObjects.Username subject = global::DomainFixture.Tests.Generation.UsernameFixture.Baseline();
            subject = global::DomainFixture.Tests.Generation.UsernameTestFactory.WithValue(subject, new string ('a', 129));
            var validator = new global::DomainFixture.Tests.Generation.UsernameValidationFluentValidationAdapter();
            var report = validator.Validate(subject);
            Assert.That(report.ContainsFailure("Value"), Is.True);
        }

        [Test]
        public void Validation_Value_NotEmpty_Empty_IsInvalid()
        {
            global::DomainFixture.Tests.Domain.ValueObjects.Username subject = global::DomainFixture.Tests.Generation.UsernameFixture.Baseline();
            subject = global::DomainFixture.Tests.Generation.UsernameTestFactory.WithValue(subject, "");
            var validator = new global::DomainFixture.Tests.Generation.UsernameValidationFluentValidationAdapter();
            var report = validator.Validate(subject);
            Assert.That(report.ContainsFailure("Value"), Is.True);
        }

        [Test]
        public void Validation_Value_NotNull_Null_IsInvalid()
        {
            global::DomainFixture.Tests.Domain.ValueObjects.Username subject = global::DomainFixture.Tests.Generation.UsernameFixture.Baseline();
            subject = global::DomainFixture.Tests.Generation.UsernameTestFactory.WithValue(subject, null);
            var validator = new global::DomainFixture.Tests.Generation.UsernameValidationFluentValidationAdapter();
            var report = validator.Validate(subject);
            Assert.That(report.ContainsFailure("Value"), Is.True);
        }
    }
}

namespace DomainFixture.Tests.Generation
{
    internal sealed class UsernameValidationFluentValidationAdapter : global::DomainFixture.Validation.IFixtureValidator<global::DomainFixture.Tests.Domain.ValueObjects.Username>
    {
        private readonly global::DomainFixture.Tests.Domain.ValueObjects.UsernameValidator _validator = new global::DomainFixture.Tests.Domain.ValueObjects.UsernameValidator();

        public global::DomainFixture.Validation.ValidationReport Validate(global::DomainFixture.Tests.Domain.ValueObjects.Username subject)
        {
            var context = new global::FluentValidation.ValidationContext<global::DomainFixture.Tests.Domain.ValueObjects.Username>(subject);
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
