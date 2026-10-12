using System.Text.RegularExpressions;

namespace DjAssistantAgenticDemo.Services;

public static class SongInputParser
{
    private static readonly Regex ByPattern = new(@"^(?<song>.+?)\s+by\s+(?<artist>.+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DashPattern = new(@"^(?<artist>.+?)\s*[-–—]\s*(?<song>.+)$", RegexOptions.Compiled);

    public static (string? Song, string? Artist) TryParse(string input)
    {
        var text = (input ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(text)) return (null, null);

        var m1 = ByPattern.Match(text);
        if (m1.Success)
            return (m1.Groups["song"].Value.Trim(), m1.Groups["artist"].Value.Trim());

        var m2 = DashPattern.Match(text);
        if (m2.Success)
            return (m2.Groups["song"].Value.Trim(), m2.Groups["artist"].Value.Trim());

        return (text, null);
    }
}
