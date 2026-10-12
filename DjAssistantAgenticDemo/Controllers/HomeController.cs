using System.Text.Json;
using DjAssistantAgenticDemo.Data;
using DjAssistantAgenticDemo.Models;
using DjAssistantAgenticDemo.Services;
using DjAssistantAgenticDemo.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DjAssistantAgenticDemo.Controllers;

public sealed class HomeController : Controller
{
    private readonly AppDbContext _db;
    private readonly GapFillerAgent _gapFiller;
    private readonly SuggestionAgent _suggestionAgent;
    private readonly ILogger<HomeController> _logger;
    public HomeController(AppDbContext db, GapFillerAgent gapFiller, SuggestionAgent suggestionAgent, ILogger<HomeController> logger)
    {
        _db = db;
        _gapFiller = gapFiller;
        _suggestionAgent = suggestionAgent;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> IndexAsync()
    {
        var suggestionsModel = await GetSuggestions(new CancellationToken());

        return View("Index", new HomeModel { Text = "", Suggestions = suggestionsModel });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(HomeModel input, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View("Index", input);

        var raw = input.Text.Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            ModelState.AddModelError(nameof(input.Text), "Please enter a song.");
            return View("Index", input);
        }

        // One input => one record
        var (songGuess, artistGuess) = SongInputParser.TryParse(raw);

        var entity = new PlayedSong
        {
            OriginalDescription = raw,
            SongName = songGuess,
            ArtistName = artistGuess,
            Status = SongStatus.Pending,
            AttemptCount = 0,
            LastUpdatedUtc = DateTime.UtcNow
        };

        _db.PlayedSongs.Add(entity);
        await _db.SaveChangesAsync(ct);

        await EnrichRecordAsync(entity.Id, isRegeneration: false, regenHint: null, ct);

        var suggestionsModel = await GetSuggestions(ct);

        return View("Index", new HomeModel { Text="", Suggestions = suggestionsModel });
    }

    private async Task<SuggestionsViewModel> GetSuggestions(CancellationToken ct)
    {
        var completedCount = await _db.PlayedSongs.CountAsync(s => s.Status == SongStatus.Completed, ct);
        var suggestions = await _db.Suggestions.OrderByDescending(s => s.CreatedUtc).ToListAsync(ct);

        return new SuggestionsViewModel
        {
            Suggestions = suggestions,
            CompletedSongsCount = completedCount
        };
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> History(CancellationToken ct)
    {
        var songs = await _db.PlayedSongs
            .OrderByDescending(s => s.LastUpdatedUtc)
            .ToListAsync(ct);

        return View(new HistoryViewModel { Songs = songs });
    }

    [HttpGet]
    public async Task<IActionResult> Suggestions(CancellationToken ct)
    {
        var input = new SuggestionsViewModel();
        return View(await GetSuggestions(ct));
    }

    // Regeneration via HTTP request for a specific record
    [HttpPost("/records/{id:guid}/regenerate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Regenerate(Guid id, [FromForm] string? fields, CancellationToken ct)
    {
        var hint = string.IsNullOrWhiteSpace(fields) ? "Regenerate artist and genre" : $"Regenerate fields: {fields}";
        _logger.LogInformation("User requested regeneration for song {RecordId} with hint: {Hint}", id, hint);
        await EnrichRecordAsync(id, isRegeneration: true, regenHint: hint, ct);
        return RedirectToAction(nameof(History));
    }

    private async Task EnrichRecordAsync(Guid id, bool isRegeneration, string? regenHint, CancellationToken ct)
    {
        var entity = await _db.PlayedSongs.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (entity is null) {
            _logger.LogWarning("Attempted to enrich song record {RecordId}, but it was not found in the database.", id);
            return;
        }

        var old = new { entity.SongName, entity.ArtistName, entity.Genre, entity.Reason, entity.Status };
        entity.AttemptCount += 1;

        GapFillResult result;
        const int maxFillAttempts = 3;
        var fillAttempt = 0;
        Exception? lastFillException = null;

        while (true)
        {
            fillAttempt++;
            try
            {
                result = await _gapFiller.FillAsync(entity, isRegeneration, regenHint, ct);
                // Log successful attempt metric
                _logger.LogInformation("GapFiller succeeded for {RecordId} on attempt {Attempt}.", id, fillAttempt);
                break;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("GapFiller canceled for {RecordId}.", id);
                throw;
            }
            catch (Exception ex)
            {
                lastFillException = ex;
                _logger.LogWarning(ex, "GapFiller attempt {Attempt} failed for {RecordId}.", fillAttempt, id);

                if (fillAttempt >= maxFillAttempts)
                {
                    // Final failure: mark for review and persist reason
                    entity.Status = SongStatus.NeedsReview;
                    entity.Reason = $"Agent error after {fillAttempt} attempts: {ex.Message}";
                    entity.LastUpdatedUtc = DateTime.UtcNow;
                    await _db.SaveChangesAsync(ct);

                    // Emit a user-visible notification and metric-like log
                    TempData["Notification"] = "Could not auto-enrich the record; it was marked for review.";
                    _logger.LogError(ex, "GapFiller ultimately failed for {RecordId} after {Attempts} attempts.", id, fillAttempt);
                    return;
                }

                // Backoff before retrying
                try
                {
                    var delayMs = 500 * (int)Math.Pow(2, fillAttempt - 1);
                    await Task.Delay(delayMs, ct);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Delay canceled while backing off for {RecordId}.", id);
                    throw;
                }
            }
        }

        // Do not overwrite artist if user clearly provided it, unless regeneration
        var userProvidedArtist = SongInputParser.TryParse(entity.OriginalDescription).Artist is not null;

        if (!string.IsNullOrWhiteSpace(result.SongName)) entity.SongName = result.SongName.Trim();
        if ((!userProvidedArtist || isRegeneration) && !string.IsNullOrWhiteSpace(result.ArtistName)) entity.ArtistName = result.ArtistName.Trim();
        if (!string.IsNullOrWhiteSpace(result.Genre)) entity.Genre = result.Genre.Trim();
        if (!string.IsNullOrWhiteSpace(result.Reason)) entity.Reason = result.Reason.Trim();

        entity.LastUpdatedUtc = DateTime.UtcNow;

        // Completion rules
        if (!string.IsNullOrWhiteSpace(entity.SongName) && !string.IsNullOrWhiteSpace(entity.Genre))
        {
            entity.ArtistName ??= "Unknown";
            entity.Status = SongStatus.Completed;
        }
        else
        {
            entity.Status = SongStatus.NeedsReview;
        }

        _db.PlayedSongAudits.Add(new PlayedSongAudit
        {
            PlayedSongId = entity.Id,
            AttemptNumber = entity.AttemptCount,
            ChangedFields = "SongName,ArtistName,Genre,Reason,Status",
            OldValuesJson = JsonSerializer.Serialize(old),
            NewValuesJson = JsonSerializer.Serialize(new { entity.SongName, entity.ArtistName, entity.Genre, entity.Reason, entity.Status }),
            Reason = isRegeneration ? "User requested regeneration" : "Initial enrichment"
        });

        await _db.SaveChangesAsync(ct);

        // Suggestion agent runs against Completed records only
        if (entity.Status == SongStatus.Completed)
        {
            await RegenerateSuggestionsAsync(ct);
        }
    }

    private async Task RegenerateSuggestionsAsync(CancellationToken ct)
    {
        var completed = await _db.PlayedSongs
            .Where(s => s.Status == SongStatus.Completed)
            .OrderBy(s => s.LastUpdatedUtc)
            .ToListAsync(ct);

        // Clear old suggestions
        _db.Suggestions.RemoveRange(_db.Suggestions);
        await _db.SaveChangesAsync(ct);

        if (completed.Count == 0) return;

        IReadOnlyList<SuggestionItem> items;
        const int maxSuggestionAttempts = 3;
        var suggestionAttempt = 0;
        Exception? lastSuggestionException = null;

        while (true)
        {
            suggestionAttempt++;
            try
            {
                items = await _suggestionAgent.SuggestAsync(completed, ct);
                _logger.LogInformation("SuggestionAgent succeeded on attempt {Attempt}.", suggestionAttempt);
                break;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Suggestion generation canceled by request.");
                throw;
            }
            catch (Exception ex)
            {
                lastSuggestionException = ex;
                _logger.LogWarning(ex, "SuggestionAgent attempt {Attempt} failed.", suggestionAttempt);

                if (suggestionAttempt >= maxSuggestionAttempts)
                {
                    _logger.LogError(ex, "SuggestionAgent ultimately failed after {Attempts} attempts.", suggestionAttempt);
                    TempData["Notification"] = "Failed to generate suggestions. Please try again later.";
                    return;
                }

                // Backoff
                try
                {
                    var delayMs = 300 * (int)Math.Pow(2, suggestionAttempt - 1);
                    await Task.Delay(delayMs, ct);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Delay canceled while backing off suggestion retries.");
                    throw;
                }
            }
        }

        foreach (var i in items)
        {
            _db.Suggestions.Add(new Suggestion
            {
                SongName = i.SongName.Trim(),
                ArtistName = i.ArtistName.Trim(),
                Genre = string.IsNullOrWhiteSpace(i.Genre) ? null : i.Genre,
                Reason = string.IsNullOrWhiteSpace(i.Reason) ? null : i.Reason
            });
        }
       _logger.LogInformation("Successfully generated and queued {Count} new suggestions for saving.", items.Count);

        await _db.SaveChangesAsync(ct);

    }
}
