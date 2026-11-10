using System.Text.Json;
using Lesson.Domain.Entities;
using Lesson.Domain.Enums;
using Lesson.Infrastructure.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Lesson.Infrastructure.Services;

public sealed class PlacementGameBankService(IApplicationDbContext db) : IPlacementGameBankService
{
    public async Task<IReadOnlyList<PlacementGameItemDto>> GetAllAdminAsync(PlacementGameKind? kind = null, CancellationToken ct = default)
    {
        var q = db.PlacementGameItems.AsNoTracking().AsQueryable();
        if (kind is { } k) q = q.Where(x => x.Kind == k);
        var list = await q.OrderBy(x => x.Kind).ThenBy(x => x.SortOrder).ThenBy(x => x.Id).ToListAsync(ct);
        return list.Select(Map).ToList();
    }

    public async Task<PlacementGameItemDto> UpsertAsync(
        long? id,
        PlacementGameKind kind,
        string? level,
        string? questionText,
        string? optionsJson,
        int? answerIndex,
        string? word,
        string? line1,
        string? line2,
        string? hint,
        long? audioContentId,
        string? speakText,
        string? storyPrompt,
        int sortOrder,
        bool isActive,
        CancellationToken ct = default)
    {
        PlacementGameItem entity;
        if (id is { } eid)
        {
            entity = await db.PlacementGameItems.FirstOrDefaultAsync(x => x.Id == eid, ct)
                ?? throw new InvalidOperationException("آیتم یافت نشد.");
        }
        else
        {
            entity = new PlacementGameItem { CreatedAt = DateTimeOffset.UtcNow };
            db.PlacementGameItems.Add(entity);
        }

        entity.Kind = kind;
        entity.Level = string.IsNullOrWhiteSpace(level) ? null : level.Trim();
        entity.QuestionText = string.IsNullOrWhiteSpace(questionText) ? null : questionText.Trim();
        entity.OptionsJson = string.IsNullOrWhiteSpace(optionsJson) ? null : optionsJson.Trim();
        entity.AnswerIndex = answerIndex;
        entity.Word = string.IsNullOrWhiteSpace(word) ? null : word.Trim().ToUpperInvariant();
        entity.Line1 = string.IsNullOrWhiteSpace(line1) ? null : line1.Trim();
        entity.Line2 = string.IsNullOrWhiteSpace(line2) ? null : line2.Trim();
        entity.Hint = string.IsNullOrWhiteSpace(hint) ? null : hint.Trim();
        entity.AudioContentId = audioContentId is 0 or null ? null : audioContentId;
        entity.SpeakText = string.IsNullOrWhiteSpace(speakText) ? null : speakText.Trim();
        entity.StoryPrompt = string.IsNullOrWhiteSpace(storyPrompt) ? null : storyPrompt.Trim();
        entity.SortOrder = sortOrder;
        entity.IsActive = isActive;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task HardDeleteAsync(long id, CancellationToken ct = default)
    {
        var e = await db.PlacementGameItems.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new InvalidOperationException("آیتم یافت نشد.");
        db.PlacementGameItems.Remove(e);
        await db.SaveChangesAsync(ct);
    }

    public object GetPublicBank()
    {
        var items = db.PlacementGameItems.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
            .ToList();

        static string[]? ParseOpts(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try { return JsonSerializer.Deserialize<string[]>(json); }
            catch { return json.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries); }
        }

        var grammar = items.Where(x => x.Kind == PlacementGameKind.Grammar).Select(x => new
        {
            id = "g" + x.Id,
            level = x.Level ?? "A1",
            q = x.QuestionText ?? "",
            opts = ParseOpts(x.OptionsJson) ?? Array.Empty<string>(),
            ans = x.AnswerIndex ?? 0
        }).ToList();

        var vocab = items.Where(x => x.Kind == PlacementGameKind.Vocab).Select(x => new
        {
            id = "v" + x.Id,
            level = x.Level ?? "A1",
            word = x.Word ?? ""
        }).ToList();

        var listening = items.Where(x => x.Kind == PlacementGameKind.Listening).Select(x => new
        {
            id = "l" + x.Id,
            line1 = x.Line1 ?? "",
            line2 = x.Line2 ?? "",
            hint = x.Hint ?? "",
            audioUrl = x.AudioContentId is { } aid ? $"/api/site/media/{aid}" : ""
        }).ToList();

        var speaking = items.Where(x => x.Kind == PlacementGameKind.Speaking).Select(x => new
        {
            id = "s" + x.Id,
            level = x.Level ?? "A1",
            text = x.SpeakText ?? "",
            story = x.StoryPrompt ?? "جمله را تکرار کن."
        }).ToList();

        return new { grammar, vocab, listening, speaking };
    }

    private static PlacementGameItemDto Map(PlacementGameItem x) => new(
        x.Id, x.Kind, x.Level, x.QuestionText, x.OptionsJson, x.AnswerIndex, x.Word,
        x.Line1, x.Line2, x.Hint, x.AudioContentId,
        x.AudioContentId is { } aid ? $"/api/site/media/{aid}" : null,
        x.SpeakText, x.StoryPrompt, x.SortOrder, x.IsActive);
}
