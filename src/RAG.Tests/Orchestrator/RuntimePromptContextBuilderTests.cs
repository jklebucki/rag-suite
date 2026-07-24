using AwesomeAssertions;
using RAG.Orchestrator.Api.Common.Prompting;

namespace RAG.Tests.Orchestrator;

public class RuntimePromptContextBuilderTests
{
    [Fact]
    public void BuildServerDateTimeContext_IncludesExplicitDateAndWeekday()
    {
        var context = RuntimePromptContextBuilder.BuildServerDateTimeContext();

        context.Should().Contain("Today according to the API server:");
        context.Should().Contain("Current server local date:");
        context.Should().Contain("Current server local day of week:");
        // The instruction on how to treat this data block now lives in the system_*.md files,
        // so the builder emits data only.
        context.Should().NotContain("Do not infer or recalculate today's date or weekday from model memory.");
    }
}
