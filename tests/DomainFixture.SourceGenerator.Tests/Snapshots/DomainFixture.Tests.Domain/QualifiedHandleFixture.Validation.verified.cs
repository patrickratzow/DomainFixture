using System.CodeDom.Compiler;
using NUnit.Framework;
using QualifiedHandle = global::DomainFixture.Tests.Domain.ValueObjects.QualifiedHandle;
using QualifiedHandleQualifiedHandleValidatorAdapter_5fca801652e9fab5 = global::DomainFixture.Modules.FluentValidation.Generated.QualifiedHandleQualifiedHandleValidatorAdapter_5fca801652e9fab5;

namespace DomainFixture.Tests.Generation
{
    [GeneratedCode("DomainFixture.TestGenerator", "1.0.0.0")]
    [TestFixture]
    public sealed class QualifiedHandleValidationGeneratedTests
    {
        [Test]
        public void Validation_Baseline_IsValid()
        {
            QualifiedHandle subject = QualifiedHandleFixture.Baseline();
            var validator = new QualifiedHandleQualifiedHandleValidatorAdapter_5fca801652e9fab5();
            var report = validator.Validate(subject);
            Assert.That(report.IsValid, Is.True);
        }

        [Test]
        public void Validation_Name_LengthAtMaximum_IsValid()
        {
            QualifiedHandle subject = QualifiedHandleFixture.Baseline();
            subject = QualifiedHandleValidationImmutableReconstruction.WithName(subject, new string ('a', 32));
            var validator = new QualifiedHandleQualifiedHandleValidatorAdapter_5fca801652e9fab5();
            var report = validator.Validate(subject);
            Assert.That(report.IsValid, Is.True);
        }

        [Test]
        public void Validation_Name_LengthAboveMaximum_IsInvalid()
        {
            QualifiedHandle subject = QualifiedHandleFixture.Baseline();
            subject = QualifiedHandleValidationImmutableReconstruction.WithName(subject, new string ('a', 33));
            var validator = new QualifiedHandleQualifiedHandleValidatorAdapter_5fca801652e9fab5();
            var report = validator.Validate(subject);
            Assert.That(report.ContainsFailure("Name"), Is.True);
        }

        [Test]
        public void Validation_Name_LengthBelowMinimum_IsInvalid()
        {
            QualifiedHandle subject = QualifiedHandleFixture.Baseline();
            subject = QualifiedHandleValidationImmutableReconstruction.WithName(subject, new string ('a', 2));
            var validator = new QualifiedHandleQualifiedHandleValidatorAdapter_5fca801652e9fab5();
            var report = validator.Validate(subject);
            Assert.That(report.ContainsFailure("Name"), Is.True);
        }

        [Test]
        public void Validation_Name_LengthAtMinimum_IsValid()
        {
            QualifiedHandle subject = QualifiedHandleFixture.Baseline();
            subject = QualifiedHandleValidationImmutableReconstruction.WithName(subject, new string ('a', 3));
            var validator = new QualifiedHandleQualifiedHandleValidatorAdapter_5fca801652e9fab5();
            var report = validator.Validate(subject);
            Assert.That(report.IsValid, Is.True);
        }

        [Test]
        public void Validation_Name_NotEmpty_Empty_IsInvalid()
        {
            QualifiedHandle subject = QualifiedHandleFixture.Baseline();
            subject = QualifiedHandleValidationImmutableReconstruction.WithName(subject, "");
            var validator = new QualifiedHandleQualifiedHandleValidatorAdapter_5fca801652e9fab5();
            var report = validator.Validate(subject);
            Assert.That(report.ContainsFailure("Name"), Is.True);
        }

        [Test]
        public void Validation_Realm_LengthAtMaximum_IsValid()
        {
            QualifiedHandle subject = QualifiedHandleFixture.Baseline();
            subject = QualifiedHandleValidationImmutableReconstruction.WithRealm(subject, new string ('a', 16));
            var validator = new QualifiedHandleQualifiedHandleValidatorAdapter_5fca801652e9fab5();
            var report = validator.Validate(subject);
            Assert.That(report.IsValid, Is.True);
        }

        [Test]
        public void Validation_Realm_LengthAboveMaximum_IsInvalid()
        {
            QualifiedHandle subject = QualifiedHandleFixture.Baseline();
            subject = QualifiedHandleValidationImmutableReconstruction.WithRealm(subject, new string ('a', 17));
            var validator = new QualifiedHandleQualifiedHandleValidatorAdapter_5fca801652e9fab5();
            var report = validator.Validate(subject);
            Assert.That(report.ContainsFailure("Realm"), Is.True);
        }

        [Test]
        public void Validation_Realm_NotEmpty_Empty_IsInvalid()
        {
            QualifiedHandle subject = QualifiedHandleFixture.Baseline();
            subject = QualifiedHandleValidationImmutableReconstruction.WithRealm(subject, "");
            var validator = new QualifiedHandleQualifiedHandleValidatorAdapter_5fca801652e9fab5();
            var report = validator.Validate(subject);
            Assert.That(report.ContainsFailure("Realm"), Is.True);
        }

        [Test]
        public void Validation_Level_BelowMinimum_IsInvalid()
        {
            QualifiedHandle subject = QualifiedHandleFixture.Baseline();
            subject = QualifiedHandleValidationImmutableReconstruction.WithLevel(subject, 0);
            var validator = new QualifiedHandleQualifiedHandleValidatorAdapter_5fca801652e9fab5();
            var report = validator.Validate(subject);
            Assert.That(report.ContainsFailure("Level"), Is.True);
        }

        [Test]
        public void Validation_Level_AtMinimum_IsValid()
        {
            QualifiedHandle subject = QualifiedHandleFixture.Baseline();
            subject = QualifiedHandleValidationImmutableReconstruction.WithLevel(subject, 1);
            var validator = new QualifiedHandleQualifiedHandleValidatorAdapter_5fca801652e9fab5();
            var report = validator.Validate(subject);
            Assert.That(report.IsValid, Is.True);
        }

        [Test]
        public void Validation_Level_AtMaximum_IsValid()
        {
            QualifiedHandle subject = QualifiedHandleFixture.Baseline();
            subject = QualifiedHandleValidationImmutableReconstruction.WithLevel(subject, 10);
            var validator = new QualifiedHandleQualifiedHandleValidatorAdapter_5fca801652e9fab5();
            var report = validator.Validate(subject);
            Assert.That(report.IsValid, Is.True);
        }

        [Test]
        public void Validation_Level_AboveMaximum_IsInvalid()
        {
            QualifiedHandle subject = QualifiedHandleFixture.Baseline();
            subject = QualifiedHandleValidationImmutableReconstruction.WithLevel(subject, 11);
            var validator = new QualifiedHandleQualifiedHandleValidatorAdapter_5fca801652e9fab5();
            var report = validator.Validate(subject);
            Assert.That(report.ContainsFailure("Level"), Is.True);
        }

        [Test]
        public void Validation_Name_NotNull_Null_IsInvalid()
        {
            QualifiedHandle subject = QualifiedHandleFixture.Baseline();
            subject = QualifiedHandleValidationImmutableReconstruction.WithName(subject, null);
            var validator = new QualifiedHandleQualifiedHandleValidatorAdapter_5fca801652e9fab5();
            var report = validator.Validate(subject);
            Assert.That(report.ContainsFailure("Name"), Is.True);
        }

        [Test]
        public void Validation_Realm_NotNull_Null_IsInvalid()
        {
            QualifiedHandle subject = QualifiedHandleFixture.Baseline();
            subject = QualifiedHandleValidationImmutableReconstruction.WithRealm(subject, null);
            var validator = new QualifiedHandleQualifiedHandleValidatorAdapter_5fca801652e9fab5();
            var report = validator.Validate(subject);
            Assert.That(report.ContainsFailure("Realm"), Is.True);
        }

        [Test]
        public void Validation_Equality_IsReflexive()
        {
            QualifiedHandle subject = QualifiedHandleFixture.Baseline();
            Assert.That(subject.Equals(subject), Is.True);
        }

        [Test]
        public void Validation_Equality_EquivalentValuesAreSymmetric()
        {
            QualifiedHandle subject = QualifiedHandleFixture.Baseline();
            QualifiedHandle equivalent = subject with {};
            Assert.That(subject.Equals(equivalent), Is.True);
            Assert.That(equivalent.Equals(subject), Is.True);
        }

        [Test]
        public void Validation_Equality_EquivalentValuesHaveSameHashCode()
        {
            QualifiedHandle subject = QualifiedHandleFixture.Baseline();
            QualifiedHandle equivalent = subject with {};
            Assert.That(subject.GetHashCode(), Is.EqualTo(equivalent.GetHashCode()));
        }

        [Test]
        public void Validation_Construction_Constructor_Name_Realm_Level_BaselineSucceeds()
        {
            QualifiedHandle baseline = QualifiedHandleFixture.Baseline();
            QualifiedHandle constructed = new QualifiedHandle(baseline.Name, baseline.Realm, baseline.Level);
            Assert.That(constructed, Is.Not.Null);
        }

        [Test]
        public void Validation_Construction_Constructor_Name_Realm_Level_ArgumentsRoundTrip()
        {
            QualifiedHandle baseline = QualifiedHandleFixture.Baseline();
            QualifiedHandle constructed = new QualifiedHandle(baseline.Name, baseline.Realm, baseline.Level);
            Assert.That(constructed.Name, Is.EqualTo(baseline.Name));
            Assert.That(constructed.Realm, Is.EqualTo(baseline.Realm));
            Assert.That(constructed.Level, Is.EqualTo(baseline.Level));
        }
    }
}

namespace DomainFixture.Tests.Generation
{
    internal static class QualifiedHandleValidationImmutableReconstruction
    {
        internal static QualifiedHandle WithName(QualifiedHandle source, string value)
        {
            return source with {Name = value};
        }

        internal static QualifiedHandle WithRealm(QualifiedHandle source, string value)
        {
            return source with {Realm = value};
        }

        internal static QualifiedHandle WithLevel(QualifiedHandle source, int value)
        {
            return source with {Level = value};
        }
    }
}
