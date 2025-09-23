using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RAG.Orchestrator.Api.Data;
using RAG.Orchestrator.Api.Models;

namespace RAG.Orchestrator.Api.Services;

public class GlobalSettingsService : IGlobalSettingsService
{
    private readonly ChatDbContext _context;
    private readonly IGlobalSettingsCache _cache;

    public GlobalSettingsService(ChatDbContext context, IGlobalSettingsCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<LlmSettings?> GetLlmSettingsAsync()
    {
        return await _cache.GetLlmSettingsAsync();
    }

    public async Task SetLlmSettingsAsync(LlmSettings settings)
    {
        await _cache.SetLlmSettingsAsync(settings, _context);
    }

    public async Task InitializeLlmSettingsAsync(IConfiguration configuration, ChatDbContext context)
    {
        var existing = await context.GlobalSettings.AnyAsync(s => s.Key == "LlmService");
        if (existing)
        {
            return; // Already initialized in database
        }

        var llmSection = configuration.GetSection("Services:LlmService");
        var settings = new LlmSettings
        {
            Url = llmSection["Url"] ?? string.Empty,
            MaxTokens = int.TryParse(llmSection["MaxTokens"], out var maxTokens) ? maxTokens : 3000,
            Temperature = double.TryParse(llmSection["Temperature"], out var temperature) ? temperature : 0.7,
            Model = llmSection["Model"] ?? string.Empty,
            IsOllama = bool.TryParse(llmSection["IsOllama"], out var isOllama) ? isOllama : true,
            TimeoutMinutes = int.TryParse(llmSection["TimeoutMinutes"], out var timeout) ? timeout : 15,
            ChatEndpoint = llmSection["ChatEndpoint"] ?? "/api/chat",
            GenerateEndpoint = llmSection["GenerateEndpoint"] ?? "/api/generate"
        };

        var jsonValue = JsonSerializer.Serialize(settings);
        var dbSetting = new GlobalSetting { Key = "LlmService", Value = jsonValue };
        context.GlobalSettings.Add(dbSetting);
        await context.SaveChangesAsync();
    }
}