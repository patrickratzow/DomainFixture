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
            var validator = global::DomainFixture.Tests.Generation.UsernameFixture.Validator();
            var report = validator.Validate(subject);
            Assert.That(report.IsValid, Is.True);
        }

        [Test]
        public void Validation_Value_LengthAtMaximum_IsValid()
        {
            global::DomainFixture.Tests.Domain.ValueObjects.Username subject = global::DomainFixture.Tests.Generation.UsernameFixture.Baseline();
            subject = global::DomainFixture.Tests.Generation.UsernameTestFactory.WithValue(subject, new string ('a', 128));
            var validator = global::DomainFixture.Tests.Generation.UsernameFixture.Validator();
            var report = validator.Validate(subject);
            Assert.That(report.IsValid, Is.True);
        }

        [Test]
        public void Validation_Value_LengthAboveMaximum_IsInvalid()
        {
            global::DomainFixture.Tests.Domain.ValueObjects.Username subject = global::DomainFixture.Tests.Generation.UsernameFixture.Baseline();
            subject = global::DomainFixture.Tests.Generation.UsernameTestFactory.WithValue(subject, new string ('a', 129));
            var validator = global::DomainFixture.Tests.Generation.UsernameFixture.Validator();
            var report = validator.Validate(subject);
            Assert.That(report.ContainsFailure("Value"), Is.True);
        }

        [Test]
        public void Validation_Value_NotEmpty_Empty_IsInvalid()
        {
            global::DomainFixture.Tests.Domain.ValueObjects.Username subject = global::DomainFixture.Tests.Generation.UsernameFixture.Baseline();
            subject = global::DomainFixture.Tests.Generation.UsernameTestFactory.WithValue(subject, "");
            var validator = global::DomainFixture.Tests.Generation.UsernameFixture.Validator();
            var report = validator.Validate(subject);
            Assert.That(report.ContainsFailure("Value"), Is.True);
        }

        [Test]
        public void Validation_Value_NotNull_Null_IsInvalid()
        {
            global::DomainFixture.Tests.Domain.ValueObjects.Username subject = global::DomainFixture.Tests.Generation.UsernameFixture.Baseline();
            subject = global::DomainFixture.Tests.Generation.UsernameTestFactory.WithValue(subject, null);
            var validator = global::DomainFixture.Tests.Generation.UsernameFixture.Validator();
            var report = validator.Validate(subject);
            Assert.That(report.ContainsFailure("Value"), Is.True);
        }
    }
}
