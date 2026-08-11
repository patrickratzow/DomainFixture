using System.CodeDom.Compiler;
using NUnit.Framework;

namespace DomainFixture.Tests.Generation
{
    [GeneratedCode("DomainFixture.TestGenerator", "1.0.0.0")]
    [TestFixture]
    public sealed class QualifiedHandleValidationGeneratedTests
    {
        [Test]
        public void Validation_Baseline_IsValid()
        {
            global::DomainFixture.Tests.Domain.ValueObjects.QualifiedHandle subject = global::DomainFixture.Tests.Generation.QualifiedHandleFixture.Baseline();
            var validator = new global::DomainFixture.Tests.Generation.QualifiedHandleValidationFluentValidationAdapter();
            var report = validator.Validate(subject);
            Assert.That(report.IsValid, Is.True);
        }

        [Test]
        public void Validation_Name_LengthAtMaximum_IsValid()
        {
            global::DomainFixture.Tests.Domain.ValueObjects.QualifiedHandle subject = global::DomainFixture.Tests.Generation.QualifiedHandleFixture.Baseline();
            subject = global::DomainFixture.Tests.Generation.QualifiedHandleValidationImmutableReconstruction.WithName(subject, new string ('a', 32));
            var validator = new global::DomainFixture.Tests.Generation.QualifiedHandleValidationFluentValidationAdapter();
            var report = validator.Validate(subject);
            Assert.That(report.IsValid, Is.True);
        }

        [Test]
        public void Validation_Name_LengthAboveMaximum_IsInvalid()
        {
            global::DomainFixture.Tests.Domain.ValueObjects.QualifiedHandle subject = global::DomainFixture.Tests.Generation.QualifiedHandleFixture.Baseline();
            subject = global::DomainFixture.Tests.Generation.QualifiedHandleValidationImmutableReconstruction.WithName(subject, new string ('a', 33));
            var validator = new global::DomainFixture.Tests.Generation.QualifiedHandleValidationFluentValidationAdapter();
            var report = validator.Validate(subject);
            Assert.That(report.ContainsFailure("Name"), Is.True);
        }

        [Test]
        public void Validation_Name_NotEmpty_Empty_IsInvalid()
        {
            global::DomainFixture.Tests.Domain.ValueObjects.QualifiedHandle subject = global::DomainFixture.Tests.Generation.QualifiedHandleFixture.Baseline();
            subject = global::DomainFixture.Tests.Generation.QualifiedHandleValidationImmutableReconstruction.WithName(subject, "");
            var validator = new global::DomainFixture.Tests.Generation.QualifiedHandleValidationFluentValidationAdapter();
            var report = validator.Validate(subject);
            Assert.That(report.ContainsFailure("Name"), Is.True);
        }

        [Test]
        public void Validation_Realm_LengthAtMaximum_IsValid()
        {
            global::DomainFixture.Tests.Domain.ValueObjects.QualifiedHandle subject = global::DomainFixture.Tests.Generation.QualifiedHandleFixture.Baseline();
            subject = global::DomainFixture.Tests.Generation.QualifiedHandleValidationImmutableReconstruction.WithRealm(subject, new string ('a', 16));
            var validator = new global::DomainFixture.Tests.Generation.QualifiedHandleValidationFluentValidationAdapter();
            var report = validator.Validate(subject);
            Assert.That(report.IsValid, Is.True);
        }

        [Test]
        public void Validation_Realm_LengthAboveMaximum_IsInvalid()
        {
            global::DomainFixture.Tests.Domain.ValueObjects.QualifiedHandle subject = global::DomainFixture.Tests.Generation.QualifiedHandleFixture.Baseline();
            subject = global::DomainFixture.Tests.Generation.QualifiedHandleValidationImmutableReconstruction.WithRealm(subject, new string ('a', 17));
            var validator = new global::DomainFixture.Tests.Generation.QualifiedHandleValidationFluentValidationAdapter();
            var report = validator.Validate(subject);
            Assert.That(report.ContainsFailure("Realm"), Is.True);
        }

        [Test]
        public void Validation_Realm_NotEmpty_Empty_IsInvalid()
        {
            global::DomainFixture.Tests.Domain.ValueObjects.QualifiedHandle subject = global::DomainFixture.Tests.Generation.QualifiedHandleFixture.Baseline();
            subject = global::DomainFixture.Tests.Generation.QualifiedHandleValidationImmutableReconstruction.WithRealm(subject, "");
            var validator = new global::DomainFixture.Tests.Generation.QualifiedHandleValidationFluentValidationAdapter();
            var report = validator.Validate(subject);
            Assert.That(report.ContainsFailure("Realm"), Is.True);
        }

        [Test]
        public void Validation_Name_NotNull_Null_IsInvalid()
        {
            global::DomainFixture.Tests.Domain.ValueObjects.QualifiedHandle subject = global::DomainFixture.Tests.Generation.QualifiedHandleFixture.Baseline();
            subject = global::DomainFixture.Tests.Generation.QualifiedHandleValidationImmutableReconstruction.WithName(subject, null);
            var validator = new global::DomainFixture.Tests.Generation.QualifiedHandleValidationFluentValidationAdapter();
            var report = validator.Validate(subject);
            Assert.That(report.ContainsFailure("Name"), Is.True);
        }

        [Test]
        public void Validation_Realm_NotNull_Null_IsInvalid()
        {
            global::DomainFixture.Tests.Domain.ValueObjects.QualifiedHandle subject = global::DomainFixture.Tests.Generation.QualifiedHandleFixture.Baseline();
            subject = global::DomainFixture.Tests.Generation.QualifiedHandleValidationImmutableReconstruction.WithRealm(subject, null);
            var validator = new global::DomainFixture.Tests.Generation.QualifiedHandleValidationFluentValidationAdapter();
            var report = validator.Validate(subject);
            Assert.That(report.ContainsFailure("Realm"), Is.True);
        }
    }
}

namespace DomainFixture.Tests.Generation
{
    internal sealed class QualifiedHandleValidationFluentValidationAdapter : global::DomainFixture.Validation.IFixtureValidator<global::DomainFixture.Tests.Domain.ValueObjects.QualifiedHandle>
    {
        private readonly global::DomainFixture.Tests.Domain.ValueObjects.QualifiedHandleValidator _validator = new global::DomainFixture.Tests.Domain.ValueObjects.QualifiedHandleValidator();

        public global::DomainFixture.Validation.ValidationReport Validate(global::DomainFixture.Tests.Domain.ValueObjects.QualifiedHandle subject)
        {
            var context = new global::FluentValidation.ValidationContext<global::DomainFixture.Tests.Domain.ValueObjects.QualifiedHandle>(subject);
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

namespace DomainFixture.Tests.Generation
{
    internal static class QualifiedHandleValidationImmutableReconstruction
    {

        internal static global::DomainFixture.Tests.Domain.ValueObjects.QualifiedHandle WithName(global::DomainFixture.Tests.Domain.ValueObjects.QualifiedHandle source, string value)
        {
            return source with { Name = value };
        }

        internal static global::DomainFixture.Tests.Domain.ValueObjects.QualifiedHandle WithRealm(global::DomainFixture.Tests.Domain.ValueObjects.QualifiedHandle source, string value)
        {
            return source with { Realm = value };
        }
    }
}
