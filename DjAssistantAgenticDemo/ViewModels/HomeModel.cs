using System.ComponentModel.DataAnnotations;

namespace DjAssistantAgenticDemo.ViewModels;

public sealed class HomeModel
{
    [Required]
    [Display(Name = "Now playing")]
    public string Text { get; set; } = string.Empty;

    public SuggestionsViewModel Suggestions { get; set; } = new SuggestionsViewModel();
}
