using System.CodeDom.Compiler;
using NUnit.Framework;
using QualifiedName = global::DomainFixture.Tests.Domain.ValueObjects.QualifiedName;
using QualifiedNameQualifiedNameValidatorAdapter_f2d81c2acfa68052 = global::DomainFixture.Modules.FluentValidation.Generated.QualifiedNameQualifiedNameValidatorAdapter_f2d81c2acfa68052;

namespace DomainFixture.Tests.Generation
{
    [GeneratedCode("DomainFixture.TestGenerator", "1.0.0.0")]
    [TestFixture]
    public sealed class QualifiedNameValidationGeneratedTests
    {
        [Test]
        public void Validation_Baseline_IsValid()
        {
            QualifiedName subject = QualifiedNameFixture.Baseline();
            var validator = new QualifiedNameQualifiedNameValidatorAdapter_f2d81c2acfa68052();
            var report = validator.Validate(subject);
            Assert.That(report.IsValid, Is.True);
        }

        [Test]
        public void Validation_Name_LengthAtMaximum_IsValid()
        {
            QualifiedName subject = QualifiedNameFixture.Baseline();
            subject = QualifiedNameValidationImmutableReconstruction.WithName(subject, new string ('a', 32));
            var validator = new QualifiedNameQualifiedNameValidatorAdapter_f2d81c2acfa68052();
            var report = validator.Validate(subject);
            Assert.That(report.IsValid, Is.True);
        }

        [Test]
        public void Validation_Name_LengthAboveMaximum_IsInvalid()
        {
            QualifiedName subject = QualifiedNameFixture.Baseline();
            subject = QualifiedNameValidationImmutableReconstruction.WithName(subject, new string ('a', 33));
            var validator = new QualifiedNameQualifiedNameValidatorAdapter_f2d81c2acfa68052();
            var report = validator.Validate(subject);
            Assert.That(report.ContainsFailure("Name"), Is.True);
        }

        [Test]
        public void Validation_Name_NotEmpty_Empty_IsInvalid()
        {
            QualifiedName subject = QualifiedNameFixture.Baseline();
            subject = QualifiedNameValidationImmutableReconstruction.WithName(subject, "");
            var validator = new QualifiedNameQualifiedNameValidatorAdapter_f2d81c2acfa68052();
            var report = validator.Validate(subject);
            Assert.That(report.ContainsFailure("Name"), Is.True);
        }

        [Test]
        public void Validation_Realm_LengthAtMaximum_IsValid()
        {
            QualifiedName subject = QualifiedNameFixture.Baseline();
            subject = QualifiedNameValidationImmutableReconstruction.WithRealm(subject, new string ('a', 16));
            var validator = new QualifiedNameQualifiedNameValidatorAdapter_f2d81c2acfa68052();
            var report = validator.Validate(subject);
            Assert.That(report.IsValid, Is.True);
        }

        [Test]
        public void Validation_Realm_LengthAboveMaximum_IsInvalid()
        {
            QualifiedName subject = QualifiedNameFixture.Baseline();
            subject = QualifiedNameValidationImmutableReconstruction.WithRealm(subject, new string ('a', 17));
            var validator = new QualifiedNameQualifiedNameValidatorAdapter_f2d81c2acfa68052();
            var report = validator.Validate(subject);
            Assert.That(report.ContainsFailure("Realm"), Is.True);
        }

        [Test]
        public void Validation_Realm_NotEmpty_Empty_IsInvalid()
        {
            QualifiedName subject = QualifiedNameFixture.Baseline();
            subject = QualifiedNameValidationImmutableReconstruction.WithRealm(subject, "");
            var validator = new QualifiedNameQualifiedNameValidatorAdapter_f2d81c2acfa68052();
            var report = validator.Validate(subject);
            Assert.That(report.ContainsFailure("Realm"), Is.True);
        }

        [Test]
        public void Validation_Name_NotNull_Null_IsInvalid()
        {
            QualifiedName subject = QualifiedNameFixture.Baseline();
            subject = QualifiedNameValidationImmutableReconstruction.WithName(subject, null);
            var validator = new QualifiedNameQualifiedNameValidatorAdapter_f2d81c2acfa68052();
            var report = validator.Validate(subject);
            Assert.That(report.ContainsFailure("Name"), Is.True);
        }

        [Test]
        public void Validation_Realm_NotNull_Null_IsInvalid()
        {
            QualifiedName subject = QualifiedNameFixture.Baseline();
            subject = QualifiedNameValidationImmutableReconstruction.WithRealm(subject, null);
            var validator = new QualifiedNameQualifiedNameValidatorAdapter_f2d81c2acfa68052();
            var report = validator.Validate(subject);
            Assert.That(report.ContainsFailure("Realm"), Is.True);
        }

        [Test]
        public void Validation_Construction_From_Name_Realm_BaselineSucceeds()
        {
            QualifiedName baseline = QualifiedNameFixture.Baseline();
            QualifiedName constructed = QualifiedName.From(baseline.Name, baseline.Realm);
            Assert.That(constructed, Is.Not.Null);
        }

        [Test]
        public void Validation_Construction_From_Name_Realm_ArgumentsRoundTrip()
        {
            QualifiedName baseline = QualifiedNameFixture.Baseline();
            QualifiedName constructed = QualifiedName.From(baseline.Name, baseline.Realm);
            Assert.That(constructed.Name, Is.EqualTo(baseline.Name));
            Assert.That(constructed.Realm, Is.EqualTo(baseline.Realm));
        }
    }
}

namespace DomainFixture.Tests.Generation
{
    internal sealed class QualifiedNameValidationImmutableReconstruction : QualifiedName
    {
        private QualifiedNameValidationImmutableReconstruction()
        {
        }

        internal static QualifiedName WithName(QualifiedName source, string value)
        {
            return new QualifiedNameValidationImmutableReconstruction{Name = value, Realm = source.Realm};
        }

        internal static QualifiedName WithRealm(QualifiedName source, string value)
        {
            return new QualifiedNameValidationImmutableReconstruction{Name = source.Name, Realm = value};
        }
    }
}
