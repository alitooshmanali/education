namespace Lesson.Infrastructure.Abstractions;

public sealed record VideoStreamResult(Stream Stream, long Length, string ContentType, string FileName);

public interface IVideoStreamingService
{
    Task<VideoStreamResult?> OpenAsync(long videoId, long userId, string deviceIdentifier, string sessionKey, bool isAdmin, CancellationToken cancellationToken = default);
}
