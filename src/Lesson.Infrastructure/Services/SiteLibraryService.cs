using Lesson.Domain.Entities;
using Lesson.Domain.Enums;
using Lesson.Infrastructure.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Lesson.Infrastructure.Services;

public sealed class SiteLibraryService(IApplicationDbContext db) : ISiteLibraryService
{
    public async Task<IReadOnlyList<SiteLibraryItemDto>> GetPublicAsync(SiteLibraryCategory? category = null, CancellationToken cancellationToken = default)
    {
        var page = await GetPublicPageAsync(category, 0, 500, null, cancellationToken);
        return page.Items;
    }

    public async Task<SiteLibraryPageDto> GetPublicPageAsync(
        SiteLibraryCategory? category, int skip, int take, long? excludeId = null, CancellationToken cancellationToken = default)
    {
        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 50);

        var q = db.SiteLibraryItems.AsNoTracking().Where(x => x.IsActive);
        if (category is { } c) q = q.Where(x => x.Category == c);
        if (excludeId is { } ex) q = q.Where(x => x.Id != ex);

        var total = await q.CountAsync(cancellationToken);
        var list = await q.OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
            .Skip(skip).Take(take)
            .ToListAsync(cancellationToken);

        return new SiteLibraryPageDto(
            list.Select(x => Map(x)).ToList(),
            total, skip, take,
            skip + list.Count < total);
    }

    public async Task<SiteLibraryItemDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var x = await db.SiteLibraryItems.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id && i.IsActive, cancellationToken);
        if (x is null) return null;

        SiteLibraryItemRefDto? prev = null;
        SiteLibraryItemRefDto? next = null;

        if (x.PreviousItemId is { } prevId)
        {
            var p = await db.SiteLibraryItems.AsNoTracking().FirstOrDefaultAsync(i => i.Id == prevId && i.IsActive, cancellationToken);
            if (p is not null) prev = MapRef(p);
        }
        if (x.NextItemId is { } nextId)
        {
            var n = await db.SiteLibraryItems.AsNoTracking().FirstOrDefaultAsync(i => i.Id == nextId && i.IsActive, cancellationToken);
            if (n is not null) next = MapRef(n);
        }

        var related = await LoadRefsAsync(ParseIds(x.RelatedItemIds), cancellationToken);
        var suggested = await LoadRefsAsync(ParseIds(x.SuggestedItemIds), cancellationToken);

        return Map(x, prev, next, related, suggested);
    }

    private async Task<List<SiteLibraryItemRefDto>> LoadRefsAsync(List<long> ids, CancellationToken ct)
    {
        if (ids.Count == 0) return new();
        var items = await db.SiteLibraryItems.AsNoTracking()
            .Where(i => ids.Contains(i.Id) && i.IsActive)
            .ToListAsync(ct);
        return ids
            .Select(rid => items.FirstOrDefault(i => i.Id == rid))
            .Where(i => i is not null)
            .Select(i => MapRef(i!))
            .ToList();
    }

    public async Task<IReadOnlyList<SiteLibraryItemDto>> GetAllAdminAsync(CancellationToken cancellationToken = default)
    {
        var list = await db.SiteLibraryItems.AsNoTracking()
            .OrderBy(x => x.Category).ThenBy(x => x.SortOrder).ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
        return list.Select(x => Map(x)).ToList();
    }

    public async Task<SiteLibraryItemDto> UpsertAsync(
        long? id, string title, string? summary, string? bodyText, SiteLibraryCategory category,
        string? imagePath, long? imageContentId, long? audioContentId, long? videoContentId,
        bool isVoiceOnly, long? previousItemId, long? nextItemId, string? relatedItemIds, string? suggestedItemIds,
        int sortOrder, bool isActive, CancellationToken cancellationToken = default)
    {
        SiteLibraryItem entity;
        if (id is { } existingId)
            entity = await db.SiteLibraryItems.FirstAsync(x => x.Id == existingId, cancellationToken);
        else
        {
            entity = new SiteLibraryItem();
            db.SiteLibraryItems.Add(entity);
        }

        entity.Title = title.Trim();
        entity.Summary = string.IsNullOrWhiteSpace(summary) ? null : summary.Trim();
        entity.BodyText = string.IsNullOrWhiteSpace(bodyText) ? null : bodyText.Trim();
        entity.Category = category;
        entity.ImagePath = string.IsNullOrWhiteSpace(imagePath) ? null : imagePath.Trim();
        entity.ImageContentId = imageContentId is 0 or null ? null : imageContentId;
        entity.AudioContentId = audioContentId is 0 or null ? null : audioContentId;
        entity.VideoContentId = videoContentId is 0 or null ? null : videoContentId;
        entity.IsVoiceOnly = isVoiceOnly;
        entity.PreviousItemId = previousItemId is 0 or null ? null : previousItemId;
        entity.NextItemId = nextItemId is 0 or null ? null : nextItemId;
        entity.RelatedItemIds = NormalizeIds(relatedItemIds);
        entity.SuggestedItemIds = NormalizeIds(suggestedItemIds);
        entity.SortOrder = sortOrder;
        entity.IsActive = isActive;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task SoftDeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await db.SiteLibraryItems.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return;
        entity.IsActive = false;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    private static string? NormalizeIds(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var ids = ParseIds(raw);
        return ids.Count == 0 ? null : string.Join(",", ids);
    }

    private static List<long> ParseIds(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return new();
        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => long.TryParse(s, out var v) ? v : 0)
            .Where(v => v > 0)
            .Distinct()
            .ToList();
    }

    private static SiteLibraryItemRefDto MapRef(SiteLibraryItem x) => new(
        x.Id, x.Title, x.Summary, ImageUrl(x), VideoUrl(x), AudioUrl(x));

    private static SiteLibraryItemDto Map(
        SiteLibraryItem x,
        SiteLibraryItemRefDto? prev = null,
        SiteLibraryItemRefDto? next = null,
        IReadOnlyList<SiteLibraryItemRefDto>? related = null,
        IReadOnlyList<SiteLibraryItemRefDto>? suggested = null) => new(
        x.Id, x.Title, x.Summary, x.BodyText, x.Category,
        ImageUrl(x), AudioUrl(x), VideoUrl(x),
        x.ImageContentId, x.AudioContentId, x.VideoContentId,
        x.IsVoiceOnly, x.PreviousItemId, x.NextItemId, x.RelatedItemIds, x.SuggestedItemIds,
        x.SortOrder, x.IsActive, prev, next, related, suggested);

    private static string? ImageUrl(SiteLibraryItem x)
    {
        if (!string.IsNullOrWhiteSpace(x.ImagePath))
            return x.ImagePath.StartsWith("/") ? x.ImagePath : "/" + x.ImagePath;
        if (x.ImageContentId is { } img) return $"/api/site/media/{img}";
        return null;
    }

    private static string? AudioUrl(SiteLibraryItem x) =>
        x.AudioContentId is { } a ? $"/api/site/media/{a}" : null;

    private static string? VideoUrl(SiteLibraryItem x) =>
        x.VideoContentId is { } v ? $"/api/site/media/{v}" : null;
}
