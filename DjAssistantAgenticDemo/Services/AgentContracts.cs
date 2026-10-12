namespace DjAssistantAgenticDemo.Services;

public sealed record GapFillResult(
    string SongName,
    string ArtistName,
    string Genre,
    string Reason,
    string Confidence
);

public sealed record SuggestionItem(
    string SongName,
    string ArtistName,
    string? Genre,
    string? Reason
);
