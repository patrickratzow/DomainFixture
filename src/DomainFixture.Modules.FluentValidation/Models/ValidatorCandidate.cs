using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace DomainFixture.Modules.FluentValidation;

internal sealed class ValidatorCandidate
{
    public string ValidatorTypeName { get; }
    public string SubjectTypeName { get; }
    public string AdapterTypeName { get; }
    public string AdapterClassName { get; }
    public ImmutableArray<RuleContribution> Rules { get; }
    public ImmutableArray<Diagnostic> Diagnostics { get; }
    public Location? Location { get; }

    public ValidatorCandidate(
        string validatorTypeName,
        string subjectTypeName,
        string adapterTypeName,
        string adapterClassName,
        ImmutableArray<RuleContribution> rules,
        ImmutableArray<Diagnostic> diagnostics,
        Location? location)
    {
        ValidatorTypeName = validatorTypeName;
        SubjectTypeName = subjectTypeName;
        AdapterTypeName = adapterTypeName;
        AdapterClassName = adapterClassName;
        Rules = rules;
        Diagnostics = diagnostics;
        Location = location;
    }
}
