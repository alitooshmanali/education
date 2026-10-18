using Lesson.Domain.Enums;

namespace Lesson.Infrastructure.Abstractions;

public sealed record SiteLibraryItemRefDto(
    long Id,
    string Title,
    string? Summary,
    string? ImageUrl,
    string? VideoUrl,
    string? AudioUrl);

public sealed record SiteLibraryItemDto(
    long Id,
    string Title,
    string? Summary,
    string? BodyText,
    SiteLibraryCategory Category,
    string? ImageUrl,
    string? AudioUrl,
    string? VideoUrl,
    long? ImageContentId,
    long? AudioContentId,
    long? VideoContentId,
    bool IsVoiceOnly,
    long? PreviousItemId,
    long? NextItemId,
    string? RelatedItemIds,
    int SortOrder,
    bool IsActive,
    SiteLibraryItemRefDto? Previous = null,
    SiteLibraryItemRefDto? Next = null,
    IReadOnlyList<SiteLibraryItemRefDto>? Related = null);

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
        long? videoContentId,
        bool isVoiceOnly,
        long? previousItemId,
        long? nextItemId,
        string? relatedItemIds,
        int sortOrder,
        bool isActive,
        CancellationToken cancellationToken = default);
    Task SoftDeleteAsync(long id, CancellationToken cancellationToken = default);
}
