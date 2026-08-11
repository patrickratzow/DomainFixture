using System;
using System.Collections.Generic;
using System.Linq;

namespace DomainFixture.Pipeline;

public enum ProviderDecisionKind
{
    NotHandled,
    Handled,
    Invalid
}

public sealed class ProviderDecision<TOutput>
{
    public ProviderDecisionKind Kind { get; }
    public TOutput? Output { get; }
    public string? Reason { get; }

    private ProviderDecision(ProviderDecisionKind kind, TOutput? output, string? reason)
    {
        Kind = kind;
        Output = output;
        Reason = reason;
    }

    public static ProviderDecision<TOutput> NotHandled() =>
        new(ProviderDecisionKind.NotHandled, default, reason: null);

    public static ProviderDecision<TOutput> Handled(TOutput output) =>
        new(ProviderDecisionKind.Handled, output, reason: null);

    public static ProviderDecision<TOutput> Invalid(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("An invalid decision requires a reason.", nameof(reason));

        return new ProviderDecision<TOutput>(ProviderDecisionKind.Invalid, default, reason);
    }
}

public interface IPipelineProvider<in TInput, TOutput>
{
    string Id { get; }

    ProviderDecision<TOutput> Evaluate(TInput input);
}

public enum ProviderPipelineMode
{
    FirstHandled,
    ExactlyOne
}

public enum ProviderResolutionKind
{
    Handled,
    Unhandled,
    Invalid,
    Ambiguous
}

public sealed class ProviderResolution<TOutput>
{
    public ProviderResolutionKind Kind { get; }
    public string? ProviderId { get; }
    public TOutput? Output { get; }
    public string? Reason { get; }
    public IReadOnlyList<string> MatchingProviderIds { get; }

    internal ProviderResolution(
        ProviderResolutionKind kind,
        string? providerId,
        TOutput? output,
        string? reason,
        IReadOnlyList<string>? matchingProviderIds = null)
    {
        Kind = kind;
        ProviderId = providerId;
        Output = output;
        Reason = reason;
        MatchingProviderIds = matchingProviderIds ?? new string[0];
    }
}

public sealed class ProviderPipeline<TInput, TOutput>
{
    private readonly IReadOnlyList<IPipelineProvider<TInput, TOutput>> _providers;
    private readonly ProviderPipelineMode _mode;

    public ProviderPipeline(
        IEnumerable<IPipelineProvider<TInput, TOutput>> providers,
        ProviderPipelineMode mode)
    {
        if (providers is null) throw new ArgumentNullException(nameof(providers));

        _providers = providers.ToArray();
        _mode = mode;
        var duplicate = _providers.GroupBy(provider => provider.Id, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1)?.Key;
        if (duplicate is not null)
            throw new ArgumentException($"Pipeline provider id '{duplicate}' is duplicated.", nameof(providers));
    }

    public ProviderResolution<TOutput> Resolve(TInput input)
    {
        var handled = new List<(string Id, TOutput Output)>();
        foreach (var provider in _providers)
        {
            var decision = provider.Evaluate(input);
            if (decision.Kind == ProviderDecisionKind.Invalid)
            {
                return new ProviderResolution<TOutput>(
                    ProviderResolutionKind.Invalid,
                    provider.Id,
                    default,
                    decision.Reason);
            }

            if (decision.Kind != ProviderDecisionKind.Handled)
                continue;

            handled.Add((provider.Id, decision.Output!));
            if (_mode == ProviderPipelineMode.FirstHandled)
            {
                return new ProviderResolution<TOutput>(
                    ProviderResolutionKind.Handled,
                    provider.Id,
                    decision.Output,
                    reason: null);
            }
        }

        if (handled.Count == 0)
        {
            return new ProviderResolution<TOutput>(
                ProviderResolutionKind.Unhandled,
                providerId: null,
                output: default,
                reason: null);
        }

        if (handled.Count > 1)
        {
            return new ProviderResolution<TOutput>(
                ProviderResolutionKind.Ambiguous,
                providerId: null,
                output: default,
                reason: null,
                handled.Select(item => item.Id).ToArray());
        }

        return new ProviderResolution<TOutput>(
            ProviderResolutionKind.Handled,
            handled[0].Id,
            handled[0].Output,
            reason: null);
    }
}
