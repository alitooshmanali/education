using Lesson.Domain.Entities;

namespace Lesson.Infrastructure.Abstractions;

public sealed record VideoDto(long Id, string FileName, string DisplayName, string RelativePath, string FileExtension, long FileSize, long FolderId, string FolderPath);
public sealed record UploadVideoRequest(long FolderId, string FileName, string DisplayName, Stream Content, long FileSize);

public interface IVideoService
{
    Task<IReadOnlyList<VideoDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VideoDto>> GetForUserAsync(long userId, bool isAdmin, CancellationToken cancellationToken = default);
    Task<Video> UploadAsync(UploadVideoRequest request, CancellationToken cancellationToken = default);
    Task MoveAsync(long videoId, long folderId, CancellationToken cancellationToken = default);
    Task DeleteAsync(long videoId, CancellationToken cancellationToken = default);
    Task<Video?> FindAsync(long videoId, CancellationToken cancellationToken = default);
}
