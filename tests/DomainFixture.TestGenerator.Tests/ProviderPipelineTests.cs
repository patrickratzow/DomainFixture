using DomainFixture.Pipeline;
using FluentAssertions;
using NUnit.Framework;

namespace DomainFixture.TestGenerator.Tests;

[TestFixture]
public sealed class ProviderPipelineTests
{
    [Test]
    public void ExactlyOne_ShouldReportAmbiguousProviders()
    {
        var pipeline = new ProviderPipeline<string, string>(
            new IPipelineProvider<string, string>[]
            {
                new PrefixProvider("first", "domain"),
                new PrefixProvider("second", "domain")
            },
            ProviderPipelineMode.ExactlyOne);

        var result = pipeline.Resolve("domainfixture");

        result.Kind.Should().Be(ProviderResolutionKind.Ambiguous);
        result.MatchingProviderIds.Should().Equal("first", "second");
    }

    [Test]
    public void FirstHandled_ShouldRespectDeclaredProviderOrder()
    {
        var pipeline = new ProviderPipeline<string, string>(
            new IPipelineProvider<string, string>[]
            {
                new PrefixProvider("specific", "domainfixture", "specific"),
                new PrefixProvider("fallback", "domain", "fallback")
            },
            ProviderPipelineMode.FirstHandled);

        var result = pipeline.Resolve("domainfixture");

        result.Kind.Should().Be(ProviderResolutionKind.Handled);
        result.ProviderId.Should().Be("specific");
        result.Output.Should().Be("specific");
    }

    [Test]
    public void InvalidDecision_ShouldStopThePipelineWithAReason()
    {
        var pipeline = new ProviderPipeline<string, string>(
            new IPipelineProvider<string, string>[]
            {
                new InvalidProvider(),
                new PrefixProvider("fallback", "domain")
            },
            ProviderPipelineMode.FirstHandled);

        var result = pipeline.Resolve("domainfixture");

        result.Kind.Should().Be(ProviderResolutionKind.Invalid);
        result.ProviderId.Should().Be("invalid");
        result.Reason.Should().Be("malformed contract");
    }

    private sealed class PrefixProvider : IPipelineProvider<string, string>
    {
        private readonly string _prefix;
        private readonly string _output;

        public PrefixProvider(string id, string prefix, string? output = null)
        {
            Id = id;
            _prefix = prefix;
            _output = output ?? id;
        }

        public string Id { get; }

        public ProviderDecision<string> Evaluate(string input) =>
            input.StartsWith(_prefix)
                ? ProviderDecision<string>.Handled(_output)
                : ProviderDecision<string>.NotHandled();
    }

    private sealed class InvalidProvider : IPipelineProvider<string, string>
    {
        public string Id => "invalid";

        public ProviderDecision<string> Evaluate(string input) =>
            ProviderDecision<string>.Invalid("malformed contract");
    }
}
