using Lesson.Domain.Entities;
using Lesson.Domain.Enums;

namespace Lesson.Infrastructure.Abstractions;

public sealed record ClassScheduleDayDto(int DayOfWeek, string DayNameFa);

public sealed record ClassRoomDto(
    long Id,
    string Code,
    string Title,
    SubjectArea Subject,
    LanguageLevel Level,
    long? TeacherId,
    string? TeacherUsername,
    int Capacity,
    int EnrolledCount,
    ClassStatus Status,
    TimeSpan StartTime,
    TimeSpan EndTime,
    IReadOnlyList<ClassScheduleDayDto> Days);

public sealed record UpsertClassRequest(
    string Code,
    string Title,
    SubjectArea Subject,
    LanguageLevel Level,
    long? TeacherId,
    int Capacity,
    TimeSpan StartTime,
    TimeSpan EndTime,
    IReadOnlyList<int> DaysOfWeek);

public interface IClassService
{
    Task<IReadOnlyList<ClassRoomDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClassRoomDto>> GetByTeacherAsync(long teacherId, CancellationToken cancellationToken = default);
    Task<ClassRoomDto?> FindAsync(long id, CancellationToken cancellationToken = default);
    Task<ClassRoom> CreateAsync(UpsertClassRequest request, CancellationToken cancellationToken = default);
    Task UpdateAsync(long id, UpsertClassRequest request, CancellationToken cancellationToken = default);
    Task AssignTeacherAsync(long classId, long? teacherId, CancellationToken cancellationToken = default);
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// بررسی تداخل زمانی: مدرس در همان روز و بازهٔ زمانی هم‌پوشان کلاس دیگری نداشته باشد.
    /// </summary>
    Task<(bool Ok, string? Reason)> ValidateTeacherScheduleAsync(
        long teacherId,
        IReadOnlyList<int> daysOfWeek,
        TimeSpan start,
        TimeSpan end,
        long? excludeClassId = null,
        CancellationToken cancellationToken = default);
}
