using Microsoft.EntityFrameworkCore;
using RAG.Orchestrator.Api.Data;
using RAG.Orchestrator.Api.Models;
using System.Text.Json;

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

    public async Task<ForumSettings?> GetForumSettingsAsync()
    {
        return await _cache.GetForumSettingsAsync();
    }

    public async Task SetLlmSettingsAsync(LlmSettings settings)
    {
        await _cache.SetLlmSettingsAsync(settings, _context);
    }

    public async Task SetForumSettingsAsync(ForumSettings settings)
    {
        await _cache.SetForumSettingsAsync(settings, _context);
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

    public async Task InitializeForumSettingsAsync(IConfiguration configuration, ChatDbContext context)
    {
        var key = "ForumSettings";
        var existing = await context.GlobalSettings.AnyAsync(s => s.Key == key);
        if (existing)
        {
            return;
        }

        var forumSection = configuration.GetSection("Services:Forum");
        var settings = new ForumSettings
        {
            EnableAttachments = bool.TryParse(forumSection["EnableAttachments"], out var enableAttachments) ? enableAttachments : true,
            MaxAttachmentCount = int.TryParse(forumSection["MaxAttachmentCount"], out var maxCount) ? maxCount : 5,
            MaxAttachmentSizeMb = int.TryParse(forumSection["MaxAttachmentSizeMb"], out var maxSize) ? maxSize : 5,
            EnableEmailNotifications = bool.TryParse(forumSection["EnableEmailNotifications"], out var enableNotifications) ? enableNotifications : true,
            BadgeRefreshSeconds = int.TryParse(forumSection["BadgeRefreshSeconds"], out var badgeSeconds) ? badgeSeconds : 60
        };

        var jsonValue = JsonSerializer.Serialize(settings);
        var dbSetting = new GlobalSetting { Key = key, Value = jsonValue };
        context.GlobalSettings.Add(dbSetting);
        await context.SaveChangesAsync();
    }
}