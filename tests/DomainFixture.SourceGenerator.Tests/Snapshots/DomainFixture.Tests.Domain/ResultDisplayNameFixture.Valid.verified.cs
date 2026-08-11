using System.CodeDom.Compiler;
using NUnit.Framework;
using System.Collections.Generic;
using IFixtureValidator = global::DomainFixture.Validation.IFixtureValidator<global::DomainFixture.Tests.Domain.ValueObjects.ResultDisplayName>;
using ResultDisplayName = global::DomainFixture.Tests.Domain.ValueObjects.ResultDisplayName;
using ResultDisplayNameValidator = global::DomainFixture.Tests.Domain.ValueObjects.ResultDisplayNameValidator;
using ValidationContext = global::FluentValidation.ValidationContext<global::DomainFixture.Tests.Domain.ValueObjects.ResultDisplayName>;
using ValidationFailure = global::DomainFixture.Validation.ValidationFailure;
using ValidationReport = global::DomainFixture.Validation.ValidationReport;

namespace DomainFixture.Tests.Generation
{
    [GeneratedCode("DomainFixture.TestGenerator", "1.0.0.0")]
    [TestFixture]
    public sealed class ResultDisplayNameValidGeneratedTests
    {
        [Test]
        public void Valid_Baseline_IsValid()
        {
            ResultDisplayName subject = ResultDisplayNameFixtureFactory.Valid.Create();
            var validator = new ResultDisplayNameValidFluentValidationAdapter();
            var report = validator.Validate(subject);
            Assert.That(report.IsValid, Is.True);
        }

        [Test]
        public void Valid_Value_LengthAtMaximum_IsValid()
        {
            ResultDisplayName subject = ResultDisplayNameFixtureFactory.Valid.Create();
            subject.Value = new string ('a', 24);
            var validator = new ResultDisplayNameValidFluentValidationAdapter();
            var report = validator.Validate(subject);
            Assert.That(report.IsValid, Is.True);
        }

        [Test]
        public void Valid_Value_LengthAboveMaximum_IsInvalid()
        {
            ResultDisplayName subject = ResultDisplayNameFixtureFactory.Valid.Create();
            subject.Value = new string ('a', 25);
            var validator = new ResultDisplayNameValidFluentValidationAdapter();
            var report = validator.Validate(subject);
            Assert.That(report.ContainsFailure("Value"), Is.True);
        }

        [Test]
        public void Valid_Value_NotEmpty_Empty_IsInvalid()
        {
            ResultDisplayName subject = ResultDisplayNameFixtureFactory.Valid.Create();
            subject.Value = "";
            var validator = new ResultDisplayNameValidFluentValidationAdapter();
            var report = validator.Validate(subject);
            Assert.That(report.ContainsFailure("Value"), Is.True);
        }

        [Test]
        public void Valid_Value_NotNull_Null_IsInvalid()
        {
            ResultDisplayName subject = ResultDisplayNameFixtureFactory.Valid.Create();
            subject.Value = null;
            var validator = new ResultDisplayNameValidFluentValidationAdapter();
            var report = validator.Validate(subject);
            Assert.That(report.ContainsFailure("Value"), Is.True);
        }

        [Test]
        public void Construction_Create_Value_LengthAboveMaximum_IsInvalid_ReturnsFailure()
        {
            ResultDisplayName baseline = ResultDisplayNameFixtureFactory.Valid.Create();
            var result = ResultDisplayName.Create(new string ('a', 25));
            Assert.That(result.IsSuccess, Is.False);
        }

        [Test]
        public void Construction_Create_Value_NotEmpty_Empty_IsInvalid_ReturnsFailure()
        {
            ResultDisplayName baseline = ResultDisplayNameFixtureFactory.Valid.Create();
            var result = ResultDisplayName.Create("");
            Assert.That(result.IsSuccess, Is.False);
        }

        [Test]
        public void Construction_Create_Value_NotNull_Null_IsInvalid_ReturnsFailure()
        {
            ResultDisplayName baseline = ResultDisplayNameFixtureFactory.Valid.Create();
            var result = ResultDisplayName.Create(null);
            Assert.That(result.IsSuccess, Is.False);
        }

        [Test]
        public void Valid_Construction_Create_Value_BaselineSucceeds()
        {
            ResultDisplayName baseline = ResultDisplayNameFixtureFactory.Valid.Create();
            var result = ResultDisplayName.Create(baseline.Value);
            Assert.That(result.IsSuccess, Is.True);
            ResultDisplayName constructed = result.Value;
            Assert.That(constructed, Is.Not.Null);
        }

        [Test]
        public void Valid_Construction_Create_Value_ArgumentsRoundTrip()
        {
            ResultDisplayName baseline = ResultDisplayNameFixtureFactory.Valid.Create();
            var result = ResultDisplayName.Create(baseline.Value);
            Assert.That(result.IsSuccess, Is.True);
            ResultDisplayName constructed = result.Value;
            Assert.That(constructed.Value, Is.EqualTo(baseline.Value));
        }
    }
}

namespace DomainFixture.Tests.Generation
{
    internal sealed class ResultDisplayNameValidFluentValidationAdapter : IFixtureValidator
    {
        private readonly ResultDisplayNameValidator _validator = new ResultDisplayNameValidator();
        public ValidationReport Validate(ResultDisplayName subject)
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
