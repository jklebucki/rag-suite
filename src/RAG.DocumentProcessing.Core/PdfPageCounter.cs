using System.Text;
using System.Text.RegularExpressions;

namespace RAG.DocumentProcessing.Core;

internal static class PdfPageCounter
{
    private static readonly Regex PageMarker = new(@"/Type\s*/Page\b", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static async Task<int> CountAsync(string path, CancellationToken cancellationToken)
    {
        var bytes = await File.ReadAllBytesAsync(path, cancellationToken);
        return PageMarker.Matches(Encoding.Latin1.GetString(bytes)).Count;
    }

    public static async Task<bool> HasPdfHeaderAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        var header = new byte[5];
        var count = await stream.ReadAsync(header, cancellationToken);
        return count == header.Length && Encoding.ASCII.GetString(header) == "%PDF-";
    }
}
