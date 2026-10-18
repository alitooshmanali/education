using Lesson.Domain.Entities;
using Lesson.Domain.Enums;
using Lesson.Infrastructure.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Lesson.Infrastructure.Services;

public sealed class SiteLibraryService(IApplicationDbContext db) : ISiteLibraryService
{
    public async Task<IReadOnlyList<SiteLibraryItemDto>> GetPublicAsync(SiteLibraryCategory? category = null, CancellationToken cancellationToken = default)
    {
        var q = db.SiteLibraryItems.AsNoTracking().Where(x => x.IsActive);
        if (category is { } c) q = q.Where(x => x.Category == c);
        var list = await q.OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToListAsync(cancellationToken);
        return list.Select(x => Map(x)).ToList();
    }

    public async Task<SiteLibraryItemDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var x = await db.SiteLibraryItems.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id && i.IsActive, cancellationToken);
        if (x is null) return null;

        SiteLibraryItemRefDto? prev = null;
        SiteLibraryItemRefDto? next = null;
        List<SiteLibraryItemRefDto> related = new();

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

        var relatedIds = ParseIds(x.RelatedItemIds);
        if (relatedIds.Count > 0)
        {
            var items = await db.SiteLibraryItems.AsNoTracking()
                .Where(i => relatedIds.Contains(i.Id) && i.IsActive)
                .ToListAsync(cancellationToken);
            related = relatedIds
                .Select(rid => items.FirstOrDefault(i => i.Id == rid))
                .Where(i => i is not null)
                .Select(i => MapRef(i!))
                .ToList();
        }

        return Map(x, prev, next, related);
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
        bool isVoiceOnly, long? previousItemId, long? nextItemId, string? relatedItemIds,
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
        IReadOnlyList<SiteLibraryItemRefDto>? related = null) => new(
        x.Id, x.Title, x.Summary, x.BodyText, x.Category,
        ImageUrl(x), AudioUrl(x), VideoUrl(x),
        x.ImageContentId, x.AudioContentId, x.VideoContentId,
        x.IsVoiceOnly, x.PreviousItemId, x.NextItemId, x.RelatedItemIds,
        x.SortOrder, x.IsActive, prev, next, related);

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
