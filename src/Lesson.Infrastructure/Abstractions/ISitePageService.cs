using Lesson.Domain.Enums;

namespace Lesson.Infrastructure.Abstractions;

public sealed record SiteSlotDto(
    long Id,
    string PageKey,
    SiteSlotType SlotType,
    string? Title,
    string? Subtitle,
    string? LinkHref,
    string? ImageUrl,
    long? ImageContentId,
    int SortOrder,
    bool IsActive);

public sealed record SitePageDto(
    string PageKey,
    IReadOnlyList<SiteSlotDto> Carousel,
    IReadOnlyList<SiteSlotDto> Circles,
    SiteSlotDto? Hero);

public interface ISitePageService
{
    Task<SitePageDto> GetPublicPageAsync(string pageKey, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SiteSlotDto>> GetAllAdminAsync(string? pageKey = null, CancellationToken cancellationToken = default);
    Task<SiteSlotDto> UpsertAsync(
        long? id,
        string pageKey,
        SiteSlotType slotType,
        string? title,
        string? subtitle,
        string? linkHref,
        long? imageContentId,
        int sortOrder,
        bool isActive,
        CancellationToken cancellationToken = default);
    Task SoftDeleteAsync(long id, CancellationToken cancellationToken = default);
    Task<bool> IsPublicMediaAsync(long contentId, CancellationToken cancellationToken = default);
}
