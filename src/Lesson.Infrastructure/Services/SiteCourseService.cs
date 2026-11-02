using Lesson.Domain;
using Lesson.Domain.Entities;
using Lesson.Domain.Enums;
using Lesson.Infrastructure.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Lesson.Infrastructure.Services;

public sealed class SiteCourseService(IApplicationDbContext db) : ISiteCourseService
{
    public async Task<IReadOnlyList<SiteCourseDto>> GetPublicAsync(CourseOfferLevel? level = null, CancellationToken ct = default)
    {
        // فقط دوره‌های فعال — حذف فقط HardDelete است
        var q = db.SiteCourses.AsNoTracking().Where(x => x.IsActive);
        if (level is { } lv) q = q.Where(x => x.Level == lv);
        var list = await q.OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToListAsync(ct);
        return list.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<SiteCourseDto>> GetAllAdminAsync(CancellationToken ct = default)
    {
        // ادمین همه را می‌بیند (حتی غیرفعال‌های قدیمی)
        var list = await db.SiteCourses.AsNoTracking()
            .OrderByDescending(x => x.IsActive)
            .ThenBy(x => x.SortOrder).ThenBy(x => x.Id)
            .ToListAsync(ct);
        return list.Select(Map).ToList();
    }

    public async Task<SiteCourseDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var x = await db.SiteCourses.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id, ct);
        return x is null ? null : Map(x);
    }

    public async Task<SiteCourseDto> UpsertAsync(
        long? id,
        string title,
        string? description,
        CourseOfferLevel level,
        decimal tuitionAmount,
        int sessionCount,
        int? sessionDurationMinutes,
        DateTimeOffset? startsAt,
        long? imageContentId,
        long? videoContentId,
        int sortOrder,
        bool isActive,
        CancellationToken ct = default)
    {
        SiteCourse entity;
        if (id is { } existingId)
        {
            entity = await db.SiteCourses.FirstOrDefaultAsync(x => x.Id == existingId, ct)
                ?? throw new InvalidOperationException("دوره یافت نشد.");
        }
        else
        {
            entity = new SiteCourse { CreatedAt = DateTimeOffset.UtcNow };
            db.SiteCourses.Add(entity);
        }

        entity.Title = title.Trim();
        entity.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        entity.Level = level;
        entity.TuitionAmount = tuitionAmount < 0 ? 0 : tuitionAmount;
        entity.SessionCount = sessionCount < 0 ? 0 : sessionCount;
        entity.SessionDurationMinutes = sessionDurationMinutes is > 0 ? sessionDurationMinutes : null;
        entity.StartsAt = startsAt;
        entity.ImageContentId = imageContentId is 0 ? null : imageContentId;
        entity.VideoContentId = videoContentId is 0 ? null : videoContentId;
        entity.SortOrder = sortOrder;
        entity.IsActive = isActive;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task SoftDeleteAsync(long id, CancellationToken ct = default)
    {
        var entity = await db.SiteCourses.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new InvalidOperationException("دوره یافت نشد.");
        entity.IsActive = false;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task HardDeleteAsync(long id, CancellationToken ct = default)
    {
        var entity = await db.SiteCourses.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new InvalidOperationException("دوره یافت نشد.");
        db.SiteCourses.Remove(entity);
        await db.SaveChangesAsync(ct);
    }

    private static SiteCourseDto Map(SiteCourse x) => new(
        x.Id,
        x.Title,
        x.Description,
        x.Level,
        LevelLabel(x.Level),
        x.TuitionAmount,
        x.SessionCount,
        x.SessionDurationMinutes,
        x.StartsAt,
        x.StartsAt is null ? null : PersianDate.FormatFull(x.StartsAt),
        x.StartsAt is null ? null : PersianDate.FormatDayName(x.StartsAt),
        x.ImageContentId is { } iid ? $"/api/site/media/{iid}" : null,
        x.VideoContentId is { } vid ? $"/api/site/media/{vid}" : null,
        x.ImageContentId,
        x.VideoContentId,
        x.SortOrder,
        x.IsActive);

    public static string LevelLabel(CourseOfferLevel l) => l switch
    {
        CourseOfferLevel.A1 => "A1 — مبتدی",
        CourseOfferLevel.A2 => "A2 — پایه",
        CourseOfferLevel.B1 => "B1 — متوسط",
        CourseOfferLevel.B2 => "B2 — متوسط رو به بالا",
        CourseOfferLevel.C1 => "C1 — پیشرفته",
        CourseOfferLevel.C2 => "C2 — مسلط",
        CourseOfferLevel.IELTS => "IELTS",
        _ => l.ToString()
    };
}
