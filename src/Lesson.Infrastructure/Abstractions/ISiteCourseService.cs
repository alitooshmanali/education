using Lesson.Domain.Enums;

namespace Lesson.Infrastructure.Abstractions;

public sealed record SiteCourseDto(
    long Id,
    string Title,
    string? Description,
    SubjectArea Subject,
    string SubjectLabel,
    CourseOfferLevel Level,
    string LevelLabel,
    decimal TuitionAmount,
    int SessionCount,
    int? SessionDurationMinutes,
    DateTimeOffset? StartsAt,
    string? StartsAtPersian,
    string? StartsDayName,
    string? ImageUrl,
    string? VideoUrl,
    long? ImageContentId,
    long? VideoContentId,
    int SortOrder,
    bool IsActive);

public interface ISiteCourseService
{
    Task<IReadOnlyList<SiteCourseDto>> GetPublicAsync(
        SubjectArea? subject = null,
        CourseOfferLevel? level = null,
        CancellationToken ct = default);

    Task<IReadOnlyList<SiteCourseDto>> GetAllAdminAsync(CancellationToken ct = default);
    Task<SiteCourseDto?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<SiteCourseDto> UpsertAsync(
        long? id,
        string title,
        string? description,
        SubjectArea subject,
        CourseOfferLevel level,
        decimal tuitionAmount,
        int sessionCount,
        int? sessionDurationMinutes,
        DateTimeOffset? startsAt,
        long? imageContentId,
        long? videoContentId,
        int sortOrder,
        bool isActive,
        CancellationToken ct = default);
    Task SoftDeleteAsync(long id, CancellationToken ct = default);
    Task HardDeleteAsync(long id, CancellationToken ct = default);
}
