namespace Lesson.Infrastructure.Abstractions;

public sealed record SiteBookDto(
    long Id,
    string Title,
    decimal Price,
    long? ImageContentId,
    string? ImageUrl,
    long? FileContentId,
    string? FileUrl,
    int SortOrder,
    bool IsActive);

public interface ISiteBookService
{
    Task<IReadOnlyList<SiteBookDto>> GetPublicAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SiteBookDto>> GetAllAdminAsync(CancellationToken ct = default);
    Task<SiteBookDto> UpsertAsync(long? id, string title, decimal price, long? imageContentId, long? fileContentId, int sortOrder, bool isActive, CancellationToken ct = default);
    Task HardDeleteAsync(long id, CancellationToken ct = default);
}
