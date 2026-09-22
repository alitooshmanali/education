using Lesson.Infrastructure.Abstractions;
using Lesson.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lesson.Infrastructure.Services;

public sealed class VideoAccessService : IVideoAccessService
{
    private readonly LessonDbContext _db;
    private readonly IPermissionService _permissions;
    private readonly IDeviceService _devices;

    public VideoAccessService(LessonDbContext db, IPermissionService permissions, IDeviceService devices)
    {
        _db = db;
        _permissions = permissions;
        _devices = devices;
    }

    public async Task<VideoAccessResult> CanWatchAsync(long userId, long videoId, string deviceIdentifier, string sessionKey, CancellationToken cancellationToken = default)
    {
        if (!await _devices.IsSessionValidAsync(userId, deviceIdentifier, sessionKey, cancellationToken))
        {
            return new VideoAccessResult(false, "Invalid session.");
        }

        var video = await _db.Videos.Include(x => x.Folder).FirstOrDefaultAsync(x => x.Id == videoId, cancellationToken);
        if (video is null || !video.IsActive)
        {
            return new VideoAccessResult(false, "Video unavailable.");
        }

        var allowed = await _permissions.HasFolderAccessAsync(userId, video.FolderId, cancellationToken);
        return allowed ? new VideoAccessResult(true, null) : new VideoAccessResult(false, "You do not have permission to watch this video.");
    }
}
