using System.CodeDom.Compiler;
using NUnit.Framework;

namespace DomainFixture.Tests.Generation
{
    [GeneratedCode("DomainFixture.TestGenerator", "1.0.0.0")]
    [TestFixture]
    public sealed class QualifiedNameValidationGeneratedTests
    {
        [Test]
        public void Validation_Baseline_IsValid()
        {
            global::DomainFixture.Tests.Domain.ValueObjects.QualifiedName subject = global::DomainFixture.Tests.Generation.QualifiedNameFixture.Baseline();
            var validator = new global::DomainFixture.Tests.Generation.QualifiedNameValidationFluentValidationAdapter();
            var report = validator.Validate(subject);
            Assert.That(report.IsValid, Is.True);
        }

        [Test]
        public void Validation_Name_LengthAtMaximum_IsValid()
        {
            global::DomainFixture.Tests.Domain.ValueObjects.QualifiedName subject = global::DomainFixture.Tests.Generation.QualifiedNameFixture.Baseline();
            subject = global::DomainFixture.Tests.Generation.QualifiedNameValidationImmutableReconstruction.WithName(subject, new string ('a', 32));
            var validator = new global::DomainFixture.Tests.Generation.QualifiedNameValidationFluentValidationAdapter();
            var report = validator.Validate(subject);
            Assert.That(report.IsValid, Is.True);
        }

        [Test]
        public void Validation_Name_LengthAboveMaximum_IsInvalid()
        {
            global::DomainFixture.Tests.Domain.ValueObjects.QualifiedName subject = global::DomainFixture.Tests.Generation.QualifiedNameFixture.Baseline();
            subject = global::DomainFixture.Tests.Generation.QualifiedNameValidationImmutableReconstruction.WithName(subject, new string ('a', 33));
            var validator = new global::DomainFixture.Tests.Generation.QualifiedNameValidationFluentValidationAdapter();
            var report = validator.Validate(subject);
            Assert.That(report.ContainsFailure("Name"), Is.True);
        }

        [Test]
        public void Validation_Name_NotEmpty_Empty_IsInvalid()
        {
            global::DomainFixture.Tests.Domain.ValueObjects.QualifiedName subject = global::DomainFixture.Tests.Generation.QualifiedNameFixture.Baseline();
            subject = global::DomainFixture.Tests.Generation.QualifiedNameValidationImmutableReconstruction.WithName(subject, "");
            var validator = new global::DomainFixture.Tests.Generation.QualifiedNameValidationFluentValidationAdapter();
            var report = validator.Validate(subject);
            Assert.That(report.ContainsFailure("Name"), Is.True);
        }

        [Test]
        public void Validation_Realm_LengthAtMaximum_IsValid()
        {
            global::DomainFixture.Tests.Domain.ValueObjects.QualifiedName subject = global::DomainFixture.Tests.Generation.QualifiedNameFixture.Baseline();
            subject = global::DomainFixture.Tests.Generation.QualifiedNameValidationImmutableReconstruction.WithRealm(subject, new string ('a', 16));
            var validator = new global::DomainFixture.Tests.Generation.QualifiedNameValidationFluentValidationAdapter();
            var report = validator.Validate(subject);
            Assert.That(report.IsValid, Is.True);
        }

        [Test]
        public void Validation_Realm_LengthAboveMaximum_IsInvalid()
        {
            global::DomainFixture.Tests.Domain.ValueObjects.QualifiedName subject = global::DomainFixture.Tests.Generation.QualifiedNameFixture.Baseline();
            subject = global::DomainFixture.Tests.Generation.QualifiedNameValidationImmutableReconstruction.WithRealm(subject, new string ('a', 17));
            var validator = new global::DomainFixture.Tests.Generation.QualifiedNameValidationFluentValidationAdapter();
            var report = validator.Validate(subject);
            Assert.That(report.ContainsFailure("Realm"), Is.True);
        }

        [Test]
        public void Validation_Realm_NotEmpty_Empty_IsInvalid()
        {
            global::DomainFixture.Tests.Domain.ValueObjects.QualifiedName subject = global::DomainFixture.Tests.Generation.QualifiedNameFixture.Baseline();
            subject = global::DomainFixture.Tests.Generation.QualifiedNameValidationImmutableReconstruction.WithRealm(subject, "");
            var validator = new global::DomainFixture.Tests.Generation.QualifiedNameValidationFluentValidationAdapter();
            var report = validator.Validate(subject);
            Assert.That(report.ContainsFailure("Realm"), Is.True);
        }

        [Test]
        public void Validation_Name_NotNull_Null_IsInvalid()
        {
            global::DomainFixture.Tests.Domain.ValueObjects.QualifiedName subject = global::DomainFixture.Tests.Generation.QualifiedNameFixture.Baseline();
            subject = global::DomainFixture.Tests.Generation.QualifiedNameValidationImmutableReconstruction.WithName(subject, null);
            var validator = new global::DomainFixture.Tests.Generation.QualifiedNameValidationFluentValidationAdapter();
            var report = validator.Validate(subject);
            Assert.That(report.ContainsFailure("Name"), Is.True);
        }

        [Test]
        public void Validation_Realm_NotNull_Null_IsInvalid()
        {
            global::DomainFixture.Tests.Domain.ValueObjects.QualifiedName subject = global::DomainFixture.Tests.Generation.QualifiedNameFixture.Baseline();
            subject = global::DomainFixture.Tests.Generation.QualifiedNameValidationImmutableReconstruction.WithRealm(subject, null);
            var validator = new global::DomainFixture.Tests.Generation.QualifiedNameValidationFluentValidationAdapter();
            var report = validator.Validate(subject);
            Assert.That(report.ContainsFailure("Realm"), Is.True);
        }
    }
}

namespace DomainFixture.Tests.Generation
{
    internal sealed class QualifiedNameValidationFluentValidationAdapter : global::DomainFixture.Validation.IFixtureValidator<global::DomainFixture.Tests.Domain.ValueObjects.QualifiedName>
    {
        private readonly global::DomainFixture.Tests.Domain.ValueObjects.QualifiedNameValidator _validator = new global::DomainFixture.Tests.Domain.ValueObjects.QualifiedNameValidator();

        public global::DomainFixture.Validation.ValidationReport Validate(global::DomainFixture.Tests.Domain.ValueObjects.QualifiedName subject)
        {
            var context = new global::FluentValidation.ValidationContext<global::DomainFixture.Tests.Domain.ValueObjects.QualifiedName>(subject);
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
    internal sealed class QualifiedNameValidationImmutableReconstruction : global::DomainFixture.Tests.Domain.ValueObjects.QualifiedName
    {
        private QualifiedNameValidationImmutableReconstruction()
        {
        }

        internal static global::DomainFixture.Tests.Domain.ValueObjects.QualifiedName WithName(global::DomainFixture.Tests.Domain.ValueObjects.QualifiedName source, string value)
        {
            return new QualifiedNameValidationImmutableReconstruction
            {
                Name = value,
                Realm = source.Realm
            };
        }

        internal static global::DomainFixture.Tests.Domain.ValueObjects.QualifiedName WithRealm(global::DomainFixture.Tests.Domain.ValueObjects.QualifiedName source, string value)
        {
            return new QualifiedNameValidationImmutableReconstruction
            {
                Name = source.Name,
                Realm = value
            };
        }
    }
}
