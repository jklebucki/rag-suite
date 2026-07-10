using AwesomeAssertions;
using RAG.Orchestrator.Api.Features.Chat;

namespace RAG.Tests.Orchestrator;

public class ChatTitleExtractorTests
{
    [Fact]
    public void Extract_MarkerOnLastLine_ReturnsTitleAndStripsLine()
    {
        var response = "## Odpowiedź\n\nKrok 1...\nCHAT_TITLE: tworzenie zamówienia zakupu";

        var (cleaned, title) = ChatTitleExtractor.Extract(response);

        title.Should().Be("tworzenie zamówienia zakupu");
        cleaned.Should().NotContain("CHAT_TITLE");
        cleaned.Should().Contain("Krok 1...");
    }

    [Fact]
    public void Extract_IsCaseInsensitive_AndTrimsAndUnwraps()
    {
        var response = "Body\nchat_title:  \"**Reset hasła i dostęp**\"  ";

        var (_, title) = ChatTitleExtractor.Extract(response);

        title.Should().Be("Reset hasła i dostęp");
    }

    [Theory]
    [InlineData("Body\nCHAT_TITLE: dwa słowa")]           // 2 words — previously rejected by exact-5 rule
    [InlineData("Body\nCHAT_TITLE: aż siedem słów w tym tytule tutaj")] // 7 words
    public void Extract_AcceptsAnyReasonableWordCount(string response)
    {
        var (_, title) = ChatTitleExtractor.Extract(response);
        title.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Extract_ToleratesTrailingLineAfterMarker()
    {
        var response = "Body\nCHAT_TITLE: temat rozmowy testowy\nDziękuję!";

        var (cleaned, title) = ChatTitleExtractor.Extract(response);

        title.Should().Be("temat rozmowy testowy");
        cleaned.Should().Contain("Dziękuję!");
        cleaned.Should().NotContain("CHAT_TITLE");
    }

    [Fact]
    public void Extract_LegacyBraceLine_IsStillSupported()
    {
        var response = "Body text\n{reset hasła i odzyskanie dostępu}";

        var (cleaned, title) = ChatTitleExtractor.Extract(response);

        title.Should().Be("reset hasła i odzyskanie dostępu");
        cleaned.Should().NotContain("{");
    }

    [Fact]
    public void Extract_DoesNotTreatMidTextCodeBraces_AsTitle()
    {
        // A brace that is not the final line (e.g. inside a code block) must not be taken as the title.
        var response = "Ustaw wartość:\n```\n{ \"key\": \"value\" }\n```\nGotowe.";

        var (_, title) = ChatTitleExtractor.Extract(response);

        title.Should().BeNull();
    }

    [Fact]
    public void Extract_NoMarker_ReturnsNullTitleAndUnchangedResponse()
    {
        var response = "## Odpowiedź\n\nZwykła treść bez znacznika.";

        var (cleaned, title) = ChatTitleExtractor.Extract(response);

        title.Should().BeNull();
        cleaned.Should().Be(response);
    }

    [Fact]
    public void Extract_PlaceholderTitle_IsRejectedButLineStripped()
    {
        var response = "Body\nCHAT_TITLE: krótki temat rozmowy";

        var (cleaned, title) = ChatTitleExtractor.Extract(response);

        title.Should().BeNull();
        cleaned.Should().NotContain("CHAT_TITLE");
    }

    [Fact]
    public void BuildFallbackTitle_UsesFirstLine_CollapsesWhitespace_AndTruncates()
    {
        ChatTitleExtractor.BuildFallbackTitle("  jak   zrobić   zamówienie\ndruga linia  ")
            .Should().Be("jak zrobić zamówienie");

        var longMessage = new string('a', 200);
        var fallback = ChatTitleExtractor.BuildFallbackTitle(longMessage);
        fallback.Length.Should().BeLessThanOrEqualTo(61); // 60 chars + ellipsis
        fallback.Should().EndWith("…");
    }

    [Fact]
    public void BuildFallbackTitle_EmptyMessage_ReturnsEmpty()
    {
        ChatTitleExtractor.BuildFallbackTitle("   ").Should().BeEmpty();
    }
}
