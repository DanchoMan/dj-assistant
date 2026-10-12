namespace DjAssistantAgenticDemo.Models;

public sealed class Suggestion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string SongName { get; set; } = string.Empty;
    public string ArtistName { get; set; } = string.Empty;
    public string? Genre { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
