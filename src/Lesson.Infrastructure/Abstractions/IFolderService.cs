using Lesson.Domain.Entities;
using Lesson.Domain.Enums;

namespace Lesson.Infrastructure.Abstractions;

public sealed record FolderNodeDto(
    long Id,
    string Name,
    string RelativePath,
    LanguageLevel Level,
    IReadOnlyList<FolderNodeDto> Children,
    IReadOnlyList<ContentDto> Contents);

public interface IFolderService
{
    Task<IReadOnlyList<FolderNodeDto>> GetTreeAsync(LanguageLevel? levelFilter = null, CancellationToken cancellationToken = default);
    Task<Folder> CreateAsync(string name, long? parentFolderId, LanguageLevel level, CancellationToken cancellationToken = default);
    Task RenameAsync(long folderId, string name, CancellationToken cancellationToken = default);
    Task DeleteAsync(long folderId, CancellationToken cancellationToken = default);
    Task<Folder?> FindAsync(long folderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Folder>> GetFlatAsync(LanguageLevel? levelFilter = null, CancellationToken cancellationToken = default);
}
