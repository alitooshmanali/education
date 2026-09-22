namespace Lesson.Infrastructure.Abstractions;

public sealed record VideoAccessResult(bool Allowed, string? Reason);

public interface IVideoAccessService
{
    Task<VideoAccessResult> CanWatchAsync(long userId, long videoId, string deviceIdentifier, string sessionKey, CancellationToken cancellationToken = default);
}
