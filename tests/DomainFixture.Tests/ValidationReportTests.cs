using DomainFixture.Validation;
using FluentAssertions;
using NUnit.Framework;

namespace DomainFixture.Tests;

[TestFixture]
public class ValidationReportTests
{
    [Test]
    public void Valid_ShouldContainNoFailures()
    {
        ValidationReport.Valid.IsValid.Should().BeTrue();
        ValidationReport.Valid.Failures.Should().BeEmpty();
    }

    [Test]
    public void ContainsFailure_ShouldMatchPropertyAndOptionalCode()
    {
        var report = new ValidationReport(new[]
        {
            new ValidationFailure("Description", "DESCRIPTION_LENGTH")
        });

        report.IsValid.Should().BeFalse();
        report.ContainsFailure("Description").Should().BeTrue();
        report.ContainsFailure("Description", "DESCRIPTION_LENGTH").Should().BeTrue();
        report.ContainsFailure("Description", "ANOTHER_CODE").Should().BeFalse();
        report.ContainsFailure("Name", "DESCRIPTION_LENGTH").Should().BeFalse();
    }
}
