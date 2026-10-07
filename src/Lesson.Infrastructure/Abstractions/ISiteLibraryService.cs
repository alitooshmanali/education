using Lesson.Domain.Enums;

namespace Lesson.Infrastructure.Abstractions;

public sealed record SiteLibraryItemDto(
    long Id,
    string Title,
    string? Summary,
    string? BodyText,
    SiteLibraryCategory Category,
    string? ImageUrl,
    string? AudioUrl,
    bool IsVoiceOnly,
    int SortOrder,
    bool IsActive);

public interface ISiteLibraryService
{
    Task<IReadOnlyList<SiteLibraryItemDto>> GetPublicAsync(SiteLibraryCategory? category = null, CancellationToken cancellationToken = default);
    Task<SiteLibraryItemDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SiteLibraryItemDto>> GetAllAdminAsync(CancellationToken cancellationToken = default);
    Task<SiteLibraryItemDto> UpsertAsync(
        long? id,
        string title,
        string? summary,
        string? bodyText,
        SiteLibraryCategory category,
        string? imagePath,
        long? imageContentId,
        long? audioContentId,
        bool isVoiceOnly,
        int sortOrder,
        bool isActive,
        CancellationToken cancellationToken = default);
    Task SoftDeleteAsync(long id, CancellationToken cancellationToken = default);
}
