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
        return list.Select(Map).ToList();
    }

    public async Task<SiteLibraryItemDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var x = await db.SiteLibraryItems.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        return x is null ? null : Map(x);
    }

    public async Task<IReadOnlyList<SiteLibraryItemDto>> GetAllAdminAsync(CancellationToken cancellationToken = default)
    {
        var list = await db.SiteLibraryItems.AsNoTracking()
            .OrderBy(x => x.Category).ThenBy(x => x.SortOrder).ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
        return list.Select(Map).ToList();
    }

    public async Task<SiteLibraryItemDto> UpsertAsync(
        long? id, string title, string? summary, string? bodyText, SiteLibraryCategory category,
        string? imagePath, long? imageContentId, long? audioContentId, bool isVoiceOnly, int sortOrder, bool isActive,
        CancellationToken cancellationToken = default)
    {
        SiteLibraryItem entity;
        if (id is { } existingId)
        {
            entity = await db.SiteLibraryItems.FirstAsync(x => x.Id == existingId, cancellationToken);
        }
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
        entity.ImageContentId = imageContentId;
        entity.AudioContentId = audioContentId;
        entity.IsVoiceOnly = isVoiceOnly;
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

    private static SiteLibraryItemDto Map(SiteLibraryItem x)
    {
        string? imageUrl = null;
        if (!string.IsNullOrWhiteSpace(x.ImagePath))
            imageUrl = x.ImagePath.StartsWith("/") ? x.ImagePath : "/" + x.ImagePath;
        else if (x.ImageContentId is { } img)
            imageUrl = $"/api/site/media/{img}";

        // Public site media endpoint (no login) — contents/stream requires auth
        string? audioUrl = x.AudioContentId is { } aud ? $"/api/site/media/{aud}" : null;

        return new SiteLibraryItemDto(
            x.Id, x.Title, x.Summary, x.BodyText, x.Category,
            imageUrl, audioUrl, x.ImageContentId, x.AudioContentId, x.IsVoiceOnly, x.SortOrder, x.IsActive);
    }
}
