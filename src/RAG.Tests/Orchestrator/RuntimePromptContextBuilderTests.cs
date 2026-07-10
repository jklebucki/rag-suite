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
        context.Should().Contain("Do not infer or recalculate today's date or weekday from model memory.");
    }
}
