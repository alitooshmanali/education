using Lesson.Domain.Entities;
using Lesson.Domain.Enums;
using Lesson.Infrastructure.Abstractions;
using Lesson.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lesson.Infrastructure.Services;

public sealed class ClassService : IClassService
{
    private static readonly string[] DayNamesFa =
        ["یک‌شنبه", "دوشنبه", "سه‌شنبه", "چهارشنبه", "پنج‌شنبه", "جمعه", "شنبه"];

    private readonly LessonDbContext _db;

    public ClassService(LessonDbContext db) => _db = db;

    public async Task<IReadOnlyList<ClassRoomDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = await _db.ClassRooms.AsNoTracking()
            .Include(x => x.Teacher)
            .Include(x => x.ScheduleDays)
            .OrderBy(x => x.Code)
            .ToListAsync(cancellationToken);
        return list.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<ClassRoomDto>> GetByTeacherAsync(long teacherId, CancellationToken cancellationToken = default)
    {
        var list = await _db.ClassRooms.AsNoTracking()
            .Include(x => x.Teacher)
            .Include(x => x.ScheduleDays)
            .Where(x => x.TeacherId == teacherId && x.IsActive)
            .OrderBy(x => x.Code)
            .ToListAsync(cancellationToken);
        return list.Select(ToDto).ToList();
    }

    public async Task<ClassRoomDto?> FindAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.ClassRooms.AsNoTracking()
            .Include(x => x.Teacher)
            .Include(x => x.ScheduleDays)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity is null ? null : ToDto(entity);
    }

    public async Task<ClassRoom> CreateAsync(UpsertClassRequest request, CancellationToken cancellationToken = default)
    {
        ValidateTimes(request);
        await EnsureUniqueCodeAsync(request.Code, null, cancellationToken);

        if (request.TeacherId is long tid)
        {
            var check = await ValidateTeacherScheduleAsync(tid, request.DaysOfWeek, request.StartTime, request.EndTime, null, cancellationToken);
            if (!check.Ok)
                throw new InvalidOperationException(check.Reason);
            await EnsureTeacherRoleAsync(tid, cancellationToken);
        }

        var entity = new ClassRoom
        {
            Code = request.Code.Trim(),
            Title = request.Title.Trim(),
            Subject = request.Subject,
            Level = request.Level,
            TeacherId = request.TeacherId,
            Capacity = Math.Max(1, request.Capacity),
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Status = request.TeacherId is null ? ClassStatus.NoTeacher : ClassStatus.Active
        };

        foreach (var d in request.DaysOfWeek.Distinct())
            entity.ScheduleDays.Add(new ClassScheduleDay { DayOfWeek = d });

        _db.ClassRooms.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        // همگام‌سازی TeacherLevels با سطح این کلاس
        if (request.TeacherId is long teacherId)
            await EnsureTeacherHasLevelAsync(teacherId, request.Level, cancellationToken);

        return entity;
    }

    public async Task UpdateAsync(long id, UpsertClassRequest request, CancellationToken cancellationToken = default)
    {
        ValidateTimes(request);
        await EnsureUniqueCodeAsync(request.Code, id, cancellationToken);

        var entity = await _db.ClassRooms.Include(x => x.ScheduleDays)
            .FirstAsync(x => x.Id == id, cancellationToken);

        if (request.TeacherId is long tid)
        {
            var check = await ValidateTeacherScheduleAsync(tid, request.DaysOfWeek, request.StartTime, request.EndTime, id, cancellationToken);
            if (!check.Ok)
                throw new InvalidOperationException(check.Reason);
            await EnsureTeacherRoleAsync(tid, cancellationToken);
        }

        entity.Code = request.Code.Trim();
        entity.Title = request.Title.Trim();
        entity.Subject = request.Subject;
        entity.Level = request.Level;
        entity.TeacherId = request.TeacherId;
        entity.Capacity = Math.Max(1, request.Capacity);
        entity.StartTime = request.StartTime;
        entity.EndTime = request.EndTime;
        entity.Status = ResolveStatus(entity);
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        entity.ScheduleDays.Clear();
        foreach (var d in request.DaysOfWeek.Distinct())
            entity.ScheduleDays.Add(new ClassScheduleDay { ClassId = id, DayOfWeek = d });

        await _db.SaveChangesAsync(cancellationToken);

        if (request.TeacherId is long teacherId)
            await EnsureTeacherHasLevelAsync(teacherId, request.Level, cancellationToken);
    }

    public async Task AssignTeacherAsync(long classId, long? teacherId, CancellationToken cancellationToken = default)
    {
        var entity = await _db.ClassRooms.Include(x => x.ScheduleDays)
            .FirstAsync(x => x.Id == classId, cancellationToken);

        if (teacherId is long tid)
        {
            await EnsureTeacherRoleAsync(tid, cancellationToken);
            var days = entity.ScheduleDays.Select(d => d.DayOfWeek).ToList();
            var check = await ValidateTeacherScheduleAsync(tid, days, entity.StartTime, entity.EndTime, classId, cancellationToken);
            if (!check.Ok)
                throw new InvalidOperationException(check.Reason);
            entity.TeacherId = tid;
            await EnsureTeacherHasLevelAsync(tid, entity.Level, cancellationToken);
        }
        else
        {
            entity.TeacherId = null;
        }

        entity.Status = ResolveStatus(entity);
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.ClassRooms.FirstAsync(x => x.Id == id, cancellationToken);
        entity.IsActive = false;
        entity.Status = ClassStatus.Inactive;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<(bool Ok, string? Reason)> ValidateTeacherScheduleAsync(
        long teacherId,
        IReadOnlyList<int> daysOfWeek,
        TimeSpan start,
        TimeSpan end,
        long? excludeClassId = null,
        CancellationToken cancellationToken = default)
    {
        if (end <= start)
            return (false, "ساعت پایان باید بعد از ساعت شروع باشد.");

        var days = daysOfWeek.Distinct().ToHashSet();
        if (days.Count == 0)
            return (false, "حداقل یک روز هفته برای کلاس لازم است.");

        var others = await _db.ClassRooms.AsNoTracking()
            .Include(x => x.ScheduleDays)
            .Where(x => x.TeacherId == teacherId && x.IsActive && x.Status != ClassStatus.Inactive)
            .Where(x => excludeClassId == null || x.Id != excludeClassId)
            .ToListAsync(cancellationToken);

        foreach (var other in others)
        {
            var sharedDays = other.ScheduleDays.Select(d => d.DayOfWeek).Where(d => days.Contains(d)).ToList();
            if (sharedDays.Count == 0)
                continue;

            if (TimesOverlap(start, end, other.StartTime, other.EndTime))
            {
                var dayFa = string.Join("، ", sharedDays.Select(d => DayNamesFa[d % 7]));
                return (false,
                    $"تداخل زمانی با کلاس «{other.Code} — {other.Title}» در روز(های) {dayFa} " +
                    $"({FormatTime(other.StartTime)} تا {FormatTime(other.EndTime)}). " +
                    "یک مدرس نمی‌تواند هم‌زمان در دو کلاس تدریس کند.");
            }
        }

        return (true, null);
    }

    private static bool TimesOverlap(TimeSpan aStart, TimeSpan aEnd, TimeSpan bStart, TimeSpan bEnd)
        => aStart < bEnd && bStart < aEnd;

    private static void ValidateTimes(UpsertClassRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            throw new InvalidOperationException("کد کلاس الزامی است.");
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new InvalidOperationException("عنوان کلاس الزامی است.");
        if (request.EndTime <= request.StartTime)
            throw new InvalidOperationException("ساعت پایان باید بعد از ساعت شروع باشد.");
        if (request.DaysOfWeek is null || request.DaysOfWeek.Count == 0)
            throw new InvalidOperationException("حداقل یک روز هفته انتخاب کنید.");
    }

    private async Task EnsureUniqueCodeAsync(string code, long? excludeId, CancellationToken ct)
    {
        var exists = await _db.ClassRooms.AnyAsync(
            x => x.Code == code.Trim() && x.IsActive && (excludeId == null || x.Id != excludeId), ct);
        if (exists)
            throw new InvalidOperationException($"کد کلاس «{code}» قبلاً استفاده شده است.");
    }

    private async Task EnsureTeacherRoleAsync(long teacherId, CancellationToken ct)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == teacherId, ct)
            ?? throw new InvalidOperationException("مدرس یافت نشد.");
        if (user.Role != UserRole.Teacher)
            throw new InvalidOperationException("کاربر انتخاب‌شده نقش مدرس ندارد.");
        if (!user.IsActive)
            throw new InvalidOperationException("مدرس غیرفعال است.");
    }

    private async Task EnsureTeacherHasLevelAsync(long teacherId, LanguageLevel level, CancellationToken ct)
    {
        var has = await _db.TeacherLevels.AnyAsync(x => x.UserId == teacherId && x.Level == level, ct);
        if (!has)
        {
            _db.TeacherLevels.Add(new TeacherLevel { UserId = teacherId, Level = level });
            await _db.SaveChangesAsync(ct);
        }
    }

    private static ClassStatus ResolveStatus(ClassRoom entity)
    {
        if (!entity.IsActive) return ClassStatus.Inactive;
        if (entity.TeacherId is null) return ClassStatus.NoTeacher;
        if (entity.EnrolledCount >= entity.Capacity) return ClassStatus.Full;
        return ClassStatus.Active;
    }

    private static string FormatTime(TimeSpan t) => $"{t.Hours:00}:{t.Minutes:00}";

    private static ClassRoomDto ToDto(ClassRoom x) => new(
        x.Id,
        x.Code,
        x.Title,
        x.Subject,
        x.Level,
        x.TeacherId,
        x.Teacher?.Username,
        x.Capacity,
        x.EnrolledCount,
        x.Status,
        x.StartTime,
        x.EndTime,
        x.ScheduleDays
            .OrderBy(d => d.DayOfWeek)
            .Select(d => new ClassScheduleDayDto(d.DayOfWeek, DayNamesFa[d.DayOfWeek % 7]))
            .ToList());
}
