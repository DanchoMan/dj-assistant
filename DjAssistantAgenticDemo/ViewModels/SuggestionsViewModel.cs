using DjAssistantAgenticDemo.Models;

namespace DjAssistantAgenticDemo.ViewModels;

public sealed class SuggestionsViewModel
{
    public IReadOnlyList<Suggestion> Suggestions { get; init; } = Array.Empty<Suggestion>();
    public int CompletedSongsCount { get; init; }
}
