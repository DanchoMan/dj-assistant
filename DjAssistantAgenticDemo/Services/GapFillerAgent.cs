using System.Text.Json;
using System.IO;
using HandlebarsDotNet;
using DjAssistantAgenticDemo.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace DjAssistantAgenticDemo.Services;

public sealed class GapFillerAgent
{
    private readonly KernelFactory _kernelFactory;
    private readonly ILogger<GapFillerAgent> _logger;
    public GapFillerAgent(KernelFactory kernelFactory, ILogger<GapFillerAgent> logger) { 
        _kernelFactory = kernelFactory;
        _logger = logger;
    }

    public async Task<GapFillResult> FillAsync(PlayedSong song, bool isRegeneration, string? regenHint, CancellationToken ct)
    {
        _logger.LogInformation("Filling gaps for song {SongId} (Regeneration: {IsRegeneration}, Hint: {RegenHint})", song.Id, isRegeneration, regenHint);
        var kernel = _kernelFactory.CreateKernel();
        var chat = kernel.GetRequiredService<IChatCompletionService>();

        // Load prompt templates from YAML and render using Handlebars.NET
        _logger.LogInformation("Loading and compiling prompt templates from {FileName}.", "GapFillerPromts.yaml");
        var prompts = PromptTemplateService.LoadPromptTemplates("GapFillerPrompts.yaml");

        var systemTemplate = Handlebars.Compile(prompts.system ?? string.Empty);
        var userTemplate = Handlebars.Compile(prompts.user ?? string.Empty);

        var model = new { song = song, isRegeneration = isRegeneration, regenHint = regenHint };
        var system = systemTemplate(model);
        var user = userTemplate(model);


        var history = new ChatHistory();
        history.AddSystemMessage(system);
        history.AddUserMessage(user);

        _logger.LogDebug("Rendered System Prompt: {SystemPrompt}", system);
        _logger.LogDebug("Rendered User Prompt: {UserPrompt}", user);

        _logger.LogInformation("Sending gap fill request to the chat service.");
        var response = await chat.GetChatMessageContentAsync(history, cancellationToken: ct);
        var json = response.Content ?? "{}";

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        static string Get(JsonElement root, string name)
            => root.TryGetProperty(name, out var p) ? (p.GetString() ?? "") : "";

        var result = new GapFillResult(
            SongName: Get(root, "SongName"),
            ArtistName: Get(root, "ArtistName"),
            Genre: Get(root, "Genre"),
            Reason: Get(root, "Reason"),
            Confidence: Get(root, "Confidence")
        );
        _logger.LogInformation("Successfully parsed LLM response for song: {SongName}", result.SongName);
        return result;
    }
}
