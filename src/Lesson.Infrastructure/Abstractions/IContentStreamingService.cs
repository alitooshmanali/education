namespace Lesson.Infrastructure.Abstractions;

public sealed record ContentStreamResult(
    Stream Stream,
    long Length,
    string ContentType,
    string FileName,
    double? ResumePositionSeconds);

public interface IContentStreamingService
{
    Task<ContentStreamResult?> OpenAsync(
        long contentId,
        long userId,
        string deviceIdentifier,
        string sessionKey,
        bool isAdmin,
        CancellationToken cancellationToken = default);
}
