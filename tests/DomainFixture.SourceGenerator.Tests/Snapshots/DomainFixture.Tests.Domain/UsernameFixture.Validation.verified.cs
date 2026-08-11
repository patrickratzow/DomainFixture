using System.CodeDom.Compiler;
using NUnit.Framework;
using System.Collections.Generic;
using IFixtureValidator = global::DomainFixture.Validation.IFixtureValidator<global::DomainFixture.Tests.Domain.ValueObjects.Username>;
using Username = global::DomainFixture.Tests.Domain.ValueObjects.Username;
using UsernameValidator = global::DomainFixture.Tests.Domain.ValueObjects.UsernameValidator;
using ValidationContext = global::FluentValidation.ValidationContext<global::DomainFixture.Tests.Domain.ValueObjects.Username>;
using ValidationException = global::FluentValidation.ValidationException;
using ValidationFailure = global::DomainFixture.Validation.ValidationFailure;
using ValidationReport = global::DomainFixture.Validation.ValidationReport;
using ValueOf = global::DomainFixture.Tests.Domain.ValueObjects.ValueOf<string, global::DomainFixture.Tests.Domain.ValueObjects.Username>;

namespace DomainFixture.Tests.Generation
{
    [GeneratedCode("DomainFixture.TestGenerator", "1.0.0.0")]
    [TestFixture]
    public sealed class UsernameValidationGeneratedTests
    {
        [Test]
        public void Validation_Baseline_IsValid()
        {
            Username subject = UsernameFixture.Baseline();
            var validator = new UsernameValidationFluentValidationAdapter();
            var report = validator.Validate(subject);
            Assert.That(report.IsValid, Is.True);
        }

        [Test]
        public void Validation_Value_LengthAtMaximum_IsValid()
        {
            Username subject = UsernameFixture.Baseline();
            subject = UsernameValidationImmutableReconstruction.WithValue(subject, new string ('a', 128));
            var validator = new UsernameValidationFluentValidationAdapter();
            var report = validator.Validate(subject);
            Assert.That(report.IsValid, Is.True);
        }

        [Test]
        public void Validation_Value_LengthAboveMaximum_IsInvalid()
        {
            Username subject = UsernameFixture.Baseline();
            subject = UsernameValidationImmutableReconstruction.WithValue(subject, new string ('a', 129));
            var validator = new UsernameValidationFluentValidationAdapter();
            var report = validator.Validate(subject);
            Assert.That(report.ContainsFailure("Value"), Is.True);
        }

        [Test]
        public void Validation_Value_NotEmpty_Empty_IsInvalid()
        {
            Username subject = UsernameFixture.Baseline();
            subject = UsernameValidationImmutableReconstruction.WithValue(subject, "");
            var validator = new UsernameValidationFluentValidationAdapter();
            var report = validator.Validate(subject);
            Assert.That(report.ContainsFailure("Value"), Is.True);
        }

        [Test]
        public void Validation_Value_NotNull_Null_IsInvalid()
        {
            Username subject = UsernameFixture.Baseline();
            subject = UsernameValidationImmutableReconstruction.WithValue(subject, null);
            var validator = new UsernameValidationFluentValidationAdapter();
            var report = validator.Validate(subject);
            Assert.That(report.ContainsFailure("Value"), Is.True);
        }

        [Test]
        public void Construction_From_Value_LengthAboveMaximum_IsInvalid_IsRejected()
        {
            Username baseline = UsernameFixture.Baseline();
            Assert.Throws<ValidationException>(() => ValueOf.From(new string ('a', 129)));
        }

        [Test]
        public void Construction_From_Value_NotEmpty_Empty_IsInvalid_IsRejected()
        {
            Username baseline = UsernameFixture.Baseline();
            Assert.Throws<ValidationException>(() => ValueOf.From(""));
        }

        [Test]
        public void Construction_From_Value_NotNull_Null_IsInvalid_IsRejected()
        {
            Username baseline = UsernameFixture.Baseline();
            Assert.Throws<ValidationException>(() => ValueOf.From(null));
        }

        [Test]
        public void Validation_Equality_IsReflexive()
        {
            Username subject = UsernameFixture.Baseline();
            Assert.That(subject.Equals(subject), Is.True);
        }

        [Test]
        public void Validation_Equality_EquivalentValuesAreSymmetric()
        {
            Username subject = UsernameFixture.Baseline();
            Username equivalent = ValueOf.From(subject.Value);
            Assert.That(subject.Equals(equivalent), Is.True);
            Assert.That(equivalent.Equals(subject), Is.True);
        }

        [Test]
        public void Validation_Equality_EquivalentValuesHaveSameHashCode()
        {
            Username subject = UsernameFixture.Baseline();
            Username equivalent = ValueOf.From(subject.Value);
            Assert.That(subject.GetHashCode(), Is.EqualTo(equivalent.GetHashCode()));
        }

        [Test]
        public void Validation_Construction_From_Value_BaselineSucceeds()
        {
            Username baseline = UsernameFixture.Baseline();
            Username constructed = ValueOf.From(baseline.Value);
            Assert.That(constructed, Is.Not.Null);
        }

        [Test]
        public void Validation_Construction_From_Value_ArgumentsRoundTrip()
        {
            Username baseline = UsernameFixture.Baseline();
            Username constructed = ValueOf.From(baseline.Value);
            Assert.That(constructed.Value, Is.EqualTo(baseline.Value));
        }
    }
}

namespace DomainFixture.Tests.Generation
{
    internal sealed class UsernameValidationFluentValidationAdapter : IFixtureValidator
    {
        private readonly UsernameValidator _validator = new UsernameValidator();
        public ValidationReport Validate(Username subject)
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

namespace DomainFixture.Tests.Generation
{
    internal sealed class UsernameValidationImmutableReconstruction : Username
    {
        private UsernameValidationImmutableReconstruction()
        {
        }

        internal static Username WithValue(Username source, string value)
        {
            return new UsernameValidationImmutableReconstruction{Value = value};
        }
    }
}
