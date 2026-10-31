using Lesson.Domain.Enums;

namespace Lesson.Infrastructure.Abstractions;

public sealed record SiteCourseDto(
    long Id,
    string Title,
    string? Description,
    CourseOfferLevel Level,
    string LevelLabel,
    decimal TuitionAmount,
    int SessionCount,
    int? SessionDurationMinutes,
    string? ImageUrl,
    string? VideoUrl,
    long? ImageContentId,
    long? VideoContentId,
    int SortOrder,
    bool IsActive);

public interface ISiteCourseService
{
    Task<IReadOnlyList<SiteCourseDto>> GetPublicAsync(CourseOfferLevel? level = null, CancellationToken ct = default);
    Task<IReadOnlyList<SiteCourseDto>> GetAllAdminAsync(CancellationToken ct = default);
    Task<SiteCourseDto?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<SiteCourseDto> UpsertAsync(
        long? id,
        string title,
        string? description,
        CourseOfferLevel level,
        decimal tuitionAmount,
        int sessionCount,
        int? sessionDurationMinutes,
        long? imageContentId,
        long? videoContentId,
        int sortOrder,
        bool isActive,
        CancellationToken ct = default);
    Task SoftDeleteAsync(long id, CancellationToken ct = default);
}
