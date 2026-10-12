namespace DjAssistantAgenticDemo.Models;

public enum SongStatus
{
    Pending = 0,
    Completed = 1,
    NeedsReview = 2
}

public sealed class PlayedSong
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Raw user input (single entry per add)
    public string OriginalDescription { get; set; } = string.Empty;

    public string? SongName { get; set; }
    public string? ArtistName { get; set; }
    public string? Genre { get; set; }

    // Agent explanation (short)
    public string? Reason { get; set; }

    public SongStatus Status { get; set; } = SongStatus.Pending;

    public int AttemptCount { get; set; } = 0;
    public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;
}
