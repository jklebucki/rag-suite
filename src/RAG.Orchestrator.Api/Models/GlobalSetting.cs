namespace RAG.Orchestrator.Api.Models;

public class GlobalSetting
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty; // JSON serialized value
}