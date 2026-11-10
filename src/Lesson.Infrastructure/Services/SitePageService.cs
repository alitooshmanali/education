using Lesson.Domain.Entities;
using Lesson.Domain.Enums;
using Lesson.Infrastructure.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Lesson.Infrastructure.Services;

public sealed class SitePageService(IApplicationDbContext db) : ISitePageService
{
    public async Task<SitePageDto> GetPublicPageAsync(string pageKey, CancellationToken cancellationToken = default)
    {
        var key = Normalize(pageKey);
        var list = await db.SitePageSlots.AsNoTracking()
            .Where(x => x.IsActive && x.PageKey == key)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

        var dtos = list.Select(Map).ToList();
        return new SitePageDto(
            key,
            dtos.Where(x => x.SlotType == SiteSlotType.Carousel).ToList(),
            dtos.Where(x => x.SlotType == SiteSlotType.Circle).ToList(),
            dtos.FirstOrDefault(x => x.SlotType == SiteSlotType.Hero));
    }

    public async Task<IReadOnlyList<SiteSlotDto>> GetAllAdminAsync(string? pageKey = null, CancellationToken cancellationToken = default)
    {
        var q = db.SitePageSlots.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(pageKey))
            q = q.Where(x => x.PageKey == Normalize(pageKey));
        var list = await q.OrderBy(x => x.PageKey).ThenBy(x => x.SlotType).ThenBy(x => x.SortOrder).ToListAsync(cancellationToken);
        return list.Select(Map).ToList();
    }

    public async Task<SiteSlotDto> UpsertAsync(
        long? id, string pageKey, SiteSlotType slotType, string? title, string? subtitle,
        string? linkHref, long? imageContentId, int sortOrder, bool isActive,
        CancellationToken cancellationToken = default)
    {
        SitePageSlot entity;
        if (id is { } eid)
            entity = await db.SitePageSlots.FirstAsync(x => x.Id == eid, cancellationToken);
        else
        {
            entity = new SitePageSlot();
            db.SitePageSlots.Add(entity);
        }

        entity.PageKey = Normalize(pageKey);
        entity.SlotType = slotType;
        entity.Title = string.IsNullOrWhiteSpace(title) ? null : title.Trim();
        entity.Subtitle = string.IsNullOrWhiteSpace(subtitle) ? null : subtitle.Trim();
        entity.LinkHref = string.IsNullOrWhiteSpace(linkHref) ? null : linkHref.Trim();
        entity.ImageContentId = imageContentId is 0 ? null : imageContentId;
        entity.SortOrder = sortOrder;
        entity.IsActive = isActive;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task SoftDeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var e = await db.SitePageSlots.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (e is null) return;
        e.IsActive = false;
        e.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> IsPublicMediaAsync(long contentId, CancellationToken cancellationToken = default)
    {
        var inSlot = await db.SitePageSlots.AnyAsync(x => x.IsActive && x.ImageContentId == contentId, cancellationToken);
        if (inSlot) return true;

        var inLibrary = await db.SiteLibraryItems.AnyAsync(x =>
            x.IsActive && (x.ImageContentId == contentId || x.AudioContentId == contentId || x.VideoContentId == contentId),
            cancellationToken);
        if (inLibrary) return true;

        // تصاویر / ویدیوی کارت دوره‌های سایت
        if (await db.SiteCourses.AnyAsync(x =>
            x.IsActive && (x.ImageContentId == contentId || x.VideoContentId == contentId),
            cancellationToken)) return true;

        if (await db.PlacementGameItems.AnyAsync(x =>
            x.IsActive && x.AudioContentId == contentId, cancellationToken)) return true;
        return await db.SiteBooks.AnyAsync(x =>
            x.IsActive && x.ImageContentId == contentId, cancellationToken);
    }

    private static string Normalize(string key) => key.Trim().ToLowerInvariant();

    private static SiteSlotDto Map(SitePageSlot x) => new(
        x.Id, x.PageKey, x.SlotType, x.Title, x.Subtitle, x.LinkHref,
        x.ImageContentId is { } cid ? $"/api/site/media/{cid}" : null,
        x.ImageContentId, x.SortOrder, x.IsActive);
}
