using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using RAG.Orchestrator.Api.Features.Search.Reranking;
using System.Net;
using System.Text;

namespace RAG.Tests.Orchestrator;

public class RerankServiceTests
{
    private static IConfiguration BuildConfig(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private static RerankService CreateService(IConfiguration config, HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler);
        return new RerankService(httpClient, config, Mock.Of<ILogger<RerankService>>());
    }

    private static Mock<HttpMessageHandler> HandlerReturning(HttpStatusCode status, string body)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
        return handler;
    }

    [Fact]
    public void IsDisabled_WhenNoUrlConfigured()
    {
        var config = BuildConfig(new Dictionary<string, string?>());
        var service = CreateService(config, new Mock<HttpMessageHandler>().Object);

        service.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void IsDisabled_WhenEnabledFalseEvenWithUrl()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Services:RerankService:Url"] = "http://rerank:80",
            ["Services:RerankService:Enabled"] = "false"
        });
        var service = CreateService(config, new Mock<HttpMessageHandler>().Object);

        service.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void RetrieveTopN_DefaultsTo40_AndHonorsConfig()
    {
        var defaultService = CreateService(BuildConfig(new Dictionary<string, string?>()), new Mock<HttpMessageHandler>().Object);
        defaultService.RetrieveTopN.Should().Be(40);

        var configured = CreateService(BuildConfig(new Dictionary<string, string?>
        {
            ["Services:RerankService:Url"] = "http://rerank:80",
            ["Services:RerankService:RetrieveTopN"] = "25"
        }), new Mock<HttpMessageHandler>().Object);
        configured.RetrieveTopN.Should().Be(25);
    }

    [Fact]
    public async Task RerankAsync_WhenDisabled_ReturnsEmptyWithoutCallingService()
    {
        var handler = HandlerReturning(HttpStatusCode.OK, "[]");
        var config = BuildConfig(new Dictionary<string, string?>()); // no URL -> disabled
        var service = CreateService(config, handler.Object);

        var hits = await service.RerankAsync("q", new[] { "a", "b" });

        hits.Should().BeEmpty();
        handler.Protected().Verify(
            "SendAsync",
            Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task RerankAsync_ParsesAndSortsHitsDescending()
    {
        // Intentionally unsorted to verify the service sorts by score desc.
        var body = "[{\"index\":0,\"score\":0.5},{\"index\":2,\"score\":0.9},{\"index\":1,\"score\":0.1}]";
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Services:RerankService:Url"] = "http://rerank:80"
        });
        var service = CreateService(config, HandlerReturning(HttpStatusCode.OK, body).Object);

        var hits = await service.RerankAsync("zamówienie zakupu", new[] { "doc0", "doc1", "doc2" });

        hits.Should().HaveCount(3);
        hits.Select(h => h.Index).Should().ContainInOrder(2, 0, 1);
        hits[0].Score.Should().Be(0.9);
    }

    [Fact]
    public async Task RerankAsync_SupportsResultsWrapper()
    {
        var body = "{\"results\":[{\"index\":1,\"score\":0.8},{\"index\":0,\"score\":0.2}]}";
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Services:RerankService:Url"] = "http://rerank:80"
        });
        var service = CreateService(config, HandlerReturning(HttpStatusCode.OK, body).Object);

        var hits = await service.RerankAsync("q", new[] { "a", "b" });

        hits.Select(h => h.Index).Should().ContainInOrder(1, 0);
    }

    [Fact]
    public async Task RerankAsync_OnHttpError_ReturnsEmptyForFallback()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Services:RerankService:Url"] = "http://rerank:80"
        });
        var service = CreateService(config, HandlerReturning(HttpStatusCode.InternalServerError, "boom").Object);

        var hits = await service.RerankAsync("q", new[] { "a", "b" });

        hits.Should().BeEmpty();
    }

    [Fact]
    public async Task RerankAsync_WithNoDocuments_ReturnsEmpty()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Services:RerankService:Url"] = "http://rerank:80"
        });
        var service = CreateService(config, HandlerReturning(HttpStatusCode.OK, "[]").Object);

        var hits = await service.RerankAsync("q", Array.Empty<string>());

        hits.Should().BeEmpty();
    }
}
