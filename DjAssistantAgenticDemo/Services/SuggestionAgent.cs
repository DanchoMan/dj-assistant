using System.Text.Json;
using System.IO;
using HandlebarsDotNet;
using DjAssistantAgenticDemo.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace DjAssistantAgenticDemo.Services;

public sealed class SuggestionAgent
{
    private readonly KernelFactory _kernelFactory;
    private readonly ILogger _logger;

     public SuggestionAgent(KernelFactory kernelFactory, ILogger<SuggestionAgent> logger)
    {
        _kernelFactory = kernelFactory;
        _logger= logger;
    }
    public async Task<IReadOnlyList<SuggestionItem>> SuggestAsync(IReadOnlyList<PlayedSong> completedSongs, CancellationToken ct)
    {
        _logger.LogInformation("Generating suggestions based on {Count} completed songs", completedSongs.Count);
        var kernel = _kernelFactory.CreateKernel();
        var chat = kernel.GetRequiredService<IChatCompletionService>();

        // Load prompt templates from YAML and render using Handlebars.NET
        _logger.LogInformation("Loading and compiling promts from {FileName}", "SuggerstionsPrompts.yaml");
        var prompts = PromptTemplateService.LoadPromptTemplates("SuggerstionsPrompts.yaml");

        var systemTemplate = Handlebars.Compile(prompts.system ?? string.Empty);
        var userTemplate = Handlebars.Compile(prompts.user ?? string.Empty);

        var model = new { completedSongs = completedSongs.OrderBy(s => s.LastUpdatedUtc).ToList() };
        var system = systemTemplate(model);
        var user = userTemplate(model);

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(system);
        chatHistory.AddUserMessage(user);

        _logger.LogDebug("System prompt after rendering: {SystemPrompt}", system);
        _logger.LogDebug("User prompt after rendering: {UserPrompt}", user);

        _logger.LogInformation("Sending suggestion request to the chat service.");
        var response = await chat.GetChatMessageContentAsync(chatHistory, cancellationToken: ct);
        var json = response.Content ?? "[]";
       
        var items = new List<SuggestionItem>();
        using var doc = JsonDocument.Parse(json);

        if (doc.RootElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var el in doc.RootElement.EnumerateArray())
            {

                string Get(string name)
                    => el.TryGetProperty(name, out var p)
                        ? (p.ValueKind == JsonValueKind.Null ? "" : (p.GetString() ?? ""))
                        : "";

                items.Add(new SuggestionItem(
                    SongName: Get("SongName"),
                    ArtistName: Get("ArtistName"),
                    Genre: el.TryGetProperty("Genre", out var g) && g.ValueKind != JsonValueKind.Null ? g.GetString() : null,
                    Reason: el.TryGetProperty("Reason", out var r) && r.ValueKind != JsonValueKind.Null ? r.GetString() : null
                ));

            }
           
        }

        var validItems = items.Where(i => !string.IsNullOrWhiteSpace(i.SongName) && !string.IsNullOrWhiteSpace(i.ArtistName))
                    .Take(3)
                    .ToList();
        _logger.LogInformation("Returning {ValidCount} valid suggestions (Filtered out {FilteredCount} invalid/extra items).",
           validItems.Count,
             items.Count - validItems.Count);
        
        return items;
    }
}
