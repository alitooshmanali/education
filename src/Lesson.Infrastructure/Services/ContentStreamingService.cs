using Lesson.Infrastructure.Abstractions;
using Lesson.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lesson.Infrastructure.Services;

public sealed class ContentStreamingService : IContentStreamingService
{
    private readonly LessonDbContext _db;
    private readonly IContentService _contents;

    public ContentStreamingService(LessonDbContext db, IContentService contents)
    {
        _db = db;
        _contents = contents;
    }

    public async Task<ContentStreamResult?> OpenAsync(
        long contentId,
        long userId,
        string deviceIdentifier,
        string sessionKey,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var content = await _db.Contents
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == contentId && x.IsActive, cancellationToken);

        if (content is null)
            return null;

        if (!isAdmin)
        {
            var hasAccess = await _db.Permissions.AnyAsync(p =>
                p.UserId == userId &&
                p.IsActive &&
                p.RevokedAt == null &&
                (p.FolderId == content.FolderId ||
                 _db.Folders.Any(f => f.Id == content.FolderId &&
                                      f.RelativePath.StartsWith(p.Folder!.RelativePath))),
                cancellationToken);

            if (!hasAccess)
                return null;
        }

        double? resume = null;
        var progress = await _contents.GetProgressAsync(userId, contentId, cancellationToken);
        if (progress is not null && !progress.IsCompleted && progress.LastPositionSeconds > 1)
        {
            resume = progress.LastPositionSeconds;
        }

        var stream = new MemoryStream(content.Content, writable: false);
        return new ContentStreamResult(stream, content.FileSize, content.MimeType, content.OriginalFileName, resume);
    }
}
