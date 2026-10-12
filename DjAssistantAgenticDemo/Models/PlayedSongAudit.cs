namespace DjAssistantAgenticDemo.Models;

public sealed class PlayedSongAudit
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PlayedSongId { get; set; }
    public int AttemptNumber { get; set; }
    public string ChangedFields { get; set; } = string.Empty;
    public string OldValuesJson { get; set; } = "{}";
    public string NewValuesJson { get; set; } = "{}";
    public string Reason { get; set; } = string.Empty;
    public DateTime UtcTimestamp { get; set; } = DateTime.UtcNow;
}
