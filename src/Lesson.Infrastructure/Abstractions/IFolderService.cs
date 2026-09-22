using Lesson.Domain.Entities;

namespace Lesson.Infrastructure.Abstractions;

public sealed record FolderNodeDto(long Id, string Name, string RelativePath, IReadOnlyList<FolderNodeDto> Children, IReadOnlyList<VideoDto> Videos);

public interface IFolderService
{
    Task<IReadOnlyList<FolderNodeDto>> GetTreeAsync(CancellationToken cancellationToken = default);
    Task<Folder> CreateAsync(string name, long? parentFolderId, CancellationToken cancellationToken = default);
    Task RenameAsync(long folderId, string name, CancellationToken cancellationToken = default);
    Task MoveAsync(long folderId, long? newParentFolderId, CancellationToken cancellationToken = default);
    Task DeleteAsync(long folderId, CancellationToken cancellationToken = default);
    Task<Folder?> FindByPathAsync(string relativePath, CancellationToken cancellationToken = default);
}
