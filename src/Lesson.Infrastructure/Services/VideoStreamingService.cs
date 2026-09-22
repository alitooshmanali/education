using Lesson.Infrastructure.Abstractions;
using Lesson.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lesson.Infrastructure.Services;

public sealed class VideoStreamingService : IVideoStreamingService
{
    private readonly LessonDbContext _db;
    private readonly IFileStorage _storage;
    private readonly IVideoAccessService _access;

    public VideoStreamingService(LessonDbContext db, IFileStorage storage, IVideoAccessService access)
    {
        _db = db;
        _storage = storage;
        _access = access;
    }

    public async Task<VideoStreamResult?> OpenAsync(long videoId, long userId, string deviceIdentifier, string sessionKey, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var video = await _db.Videos.FirstOrDefaultAsync(x => x.Id == videoId, cancellationToken);
        if (video is null || !video.IsActive)
        {
            return null;
        }

        if (!isAdmin)
        {
            var access = await _access.CanWatchAsync(userId, videoId, deviceIdentifier, sessionKey, cancellationToken);
            if (!access.Allowed)
            {
                return null;
            }
        }

        var stream = await _storage.OpenReadAsync(video.RelativePath, cancellationToken);
        var contentType = video.FileExtension.Equals(".mp3", StringComparison.OrdinalIgnoreCase) ? "audio/mpeg" : "video/x-matroska";
        return new VideoStreamResult(stream, stream.Length, contentType, video.FileName);
    }
}
