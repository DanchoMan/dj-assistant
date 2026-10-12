using DjAssistantAgenticDemo.Models;

namespace DjAssistantAgenticDemo.ViewModels;

public sealed class HistoryViewModel
{
    public IReadOnlyList<PlayedSong> Songs { get; init; } = Array.Empty<PlayedSong>();
}
