using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RAG.Orchestrator.Api.Data;
using RAG.Orchestrator.Api.Models;

namespace RAG.Orchestrator.Api.Services;

public class GlobalSettingsService : IGlobalSettingsService
{
    private readonly ChatDbContext _context;

    public GlobalSettingsService(ChatDbContext context)
    {
        _context = context;
    }

    public async Task<LlmSettings?> GetLlmSettingsAsync()
    {
        var setting = await _context.GlobalSettings
            .FirstOrDefaultAsync(s => s.Key == "LlmService");

        if (setting == null || string.IsNullOrEmpty(setting.Value))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<LlmSettings>(setting.Value);
        }
        catch
        {
            return null;
        }
    }

    public async Task SetLlmSettingsAsync(LlmSettings settings)
    {
        var setting = await _context.GlobalSettings
            .FirstOrDefaultAsync(s => s.Key == "LlmService");

        var jsonValue = JsonSerializer.Serialize(settings);

        if (setting == null)
        {
            setting = new GlobalSetting
            {
                Key = "LlmService",
                Value = jsonValue
            };
            _context.GlobalSettings.Add(setting);
        }
        else
        {
            setting.Value = jsonValue;
        }

        await _context.SaveChangesAsync();
        
        // Note: LLM service cache will refresh automatically after 5 minutes
        // No need to clear it immediately as settings changes are not frequent
    }

    public async Task InitializeLlmSettingsAsync(IConfiguration configuration)
    {
        var existing = await GetLlmSettingsAsync();
        if (existing != null)
        {
            return; // Already initialized
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

        await SetLlmSettingsAsync(settings);
    }
}