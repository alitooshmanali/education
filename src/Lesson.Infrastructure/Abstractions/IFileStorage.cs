namespace Lesson.Infrastructure.Abstractions;

public sealed record StoredFileInfo(string RelativePath, string FileName, long Length, DateTimeOffset? LastModifiedUtc = null);

public interface IFileStorage
{
    Task<StoredFileInfo> SaveAsync(string relativePath, Stream content, CancellationToken cancellationToken = default);
    Task<Stream> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default);
    Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default);
    Task MoveAsync(string sourceRelativePath, string destinationRelativePath, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string relativePath, CancellationToken cancellationToken = default);
    IAsyncEnumerable<StoredFileInfo> EnumerateAsync(string? prefix = null, CancellationToken cancellationToken = default);
}
