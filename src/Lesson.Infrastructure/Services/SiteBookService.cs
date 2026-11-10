using Lesson.Domain.Entities;
using Lesson.Infrastructure.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Lesson.Infrastructure.Services;

public sealed class SiteBookService(IApplicationDbContext db) : ISiteBookService
{
    public async Task<IReadOnlyList<SiteBookDto>> GetPublicAsync(CancellationToken ct = default)
    {
        var list = await db.SiteBooks.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
            .ToListAsync(ct);
        return list.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<SiteBookDto>> GetAllAdminAsync(CancellationToken ct = default)
    {
        var list = await db.SiteBooks.AsNoTracking()
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
            .ToListAsync(ct);
        return list.Select(Map).ToList();
    }

    public async Task<SiteBookDto> UpsertAsync(long? id, string title, decimal price, long? imageContentId, int sortOrder, bool isActive, CancellationToken ct = default)
    {
        SiteBook entity;
        if (id is { } eid)
        {
            entity = await db.SiteBooks.FirstOrDefaultAsync(x => x.Id == eid, ct)
                ?? throw new InvalidOperationException("کتاب یافت نشد.");
        }
        else
        {
            entity = new SiteBook { CreatedAt = DateTimeOffset.UtcNow };
            db.SiteBooks.Add(entity);
        }

        entity.Title = title.Trim();
        entity.Price = price < 0 ? 0 : price;
        entity.ImageContentId = imageContentId is 0 or null ? null : imageContentId;
        entity.SortOrder = sortOrder;
        entity.IsActive = isActive;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task HardDeleteAsync(long id, CancellationToken ct = default)
    {
        var e = await db.SiteBooks.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new InvalidOperationException("کتاب یافت نشد.");
        db.SiteBooks.Remove(e);
        await db.SaveChangesAsync(ct);
    }

    private static SiteBookDto Map(SiteBook x) => new(
        x.Id, x.Title, x.Price, x.ImageContentId,
        x.ImageContentId is { } iid ? $"/api/site/media/{iid}" : null,
        x.SortOrder, x.IsActive);
}
