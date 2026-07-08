using System.Globalization;
using System.Text;

namespace RAG.Orchestrator.Api.Common.Prompting;

/// <summary>
/// Builds runtime prompt context with current API server date/time and region information.
/// </summary>
public static class RuntimePromptContextBuilder
{
    public static string BuildServerDateTimeContext()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var serverTimeZone = TimeZoneInfo.Local;
        var localNow = TimeZoneInfo.ConvertTime(utcNow, serverTimeZone);
        var localDayOfWeek = localNow.ToString("dddd", CultureInfo.InvariantCulture);
        var utcDayOfWeek = utcNow.ToString("dddd", CultureInfo.InvariantCulture);

        return new StringBuilder()
            .AppendLine("=== SERVER DATE/TIME CONTEXT (API) ===")
            .AppendLine($"- Today according to the API server: {localNow:yyyy-MM-dd} ({localDayOfWeek})")
            .AppendLine($"- Current server local date: {localNow:yyyy-MM-dd}")
            .AppendLine($"- Current server local day of week: {localDayOfWeek}")
            .AppendLine($"- Current UTC datetime: {utcNow:yyyy-MM-dd HH:mm:ss 'UTC'}")
            .AppendLine($"- Current UTC day of week: {utcDayOfWeek}")
            .AppendLine($"- Current server local datetime: {localNow:yyyy-MM-dd HH:mm:ss zzz}")
            .AppendLine($"- Server timezone: {serverTimeZone.Id} ({GetTimeZoneDisplayName(serverTimeZone, localNow.DateTime)})")
            .AppendLine($"- API server region: {GetRegionDisplayName()}")
            .Append("- Treat this as authoritative current date/time. Do not infer or recalculate today's date or weekday from model memory.")
            .ToString();
    }

    private static string GetTimeZoneDisplayName(TimeZoneInfo timeZone, DateTime localNow)
    {
        var offset = timeZone.GetUtcOffset(localNow);
        var sign = offset < TimeSpan.Zero ? "-" : "+";
        var absoluteOffset = offset.Duration();
        var offsetLabel = $"{sign}{absoluteOffset:hh\\:mm}";
        var timeZoneName = timeZone.IsDaylightSavingTime(localNow)
            ? timeZone.DaylightName
            : timeZone.StandardName;

        return $"{timeZoneName}, UTC{offsetLabel}";
    }

    private static string GetRegionDisplayName()
    {
        try
        {
            var region = RegionInfo.CurrentRegion;
            return $"{region.EnglishName} ({region.Name})";
        }
        catch
        {
            return "Unknown";
        }
    }
}
