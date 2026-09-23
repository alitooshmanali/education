using Lesson.Domain.Entities;
using Lesson.Domain.Enums;
using Lesson.Infrastructure.Abstractions;
using Lesson.Infrastructure.Options;
using Lesson.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Lesson.Infrastructure.Services;

public sealed class ContentService : IContentService
{
    public const long MaxUploadBytes = 20L * 1024 * 1024; // 20 MB

    private readonly LessonDbContext _db;
    private readonly LessonStorageOptions _options;

    public ContentService(LessonDbContext db, IOptions<LessonStorageOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<ContentDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        // IMPORTANT: never select Content (varbinary) here — only metadata
        var list = await _db.Contents.AsNoTracking()
            .OrderBy(x => x.DisplayName)
            .Select(x => new ContentDto(
                x.Id,
                x.OriginalFileName,
                x.DisplayName,
                x.FileExtension,
                x.MimeType,
                x.FileSize,
                x.Type,
                x.DurationSeconds,
                x.FolderId,
                x.Folder != null ? x.Folder.RelativePath : string.Empty))
            .ToListAsync(cancellationToken);
        return list;
    }

    public async Task<IReadOnlyList<ContentDto>> GetForUserAsync(long userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        if (isAdmin)
        {
            return await GetAllAsync(cancellationToken);
        }

        // Permission on folder or any ancestor (path prefix) — metadata only, no Content bytes
        var list = await (
            from c in _db.Contents.AsNoTracking()
            where _db.Permissions.Any(p =>
                p.UserId == userId &&
                p.IsActive &&
                p.RevokedAt == null &&
                (c.FolderId == p.FolderId ||
                 _db.Folders.Any(f => f.Id == c.FolderId && f.RelativePath.StartsWith(p.Folder!.RelativePath))))
            orderby c.DisplayName
            select new ContentDto(
                c.Id,
                c.OriginalFileName,
                c.DisplayName,
                c.FileExtension,
                c.MimeType,
                c.FileSize,
                c.Type,
                c.DurationSeconds,
                c.FolderId,
                c.Folder != null ? c.Folder.RelativePath : string.Empty)
        ).ToListAsync(cancellationToken);

        return list;
    }

    public Task<LessonContent?> FindAsync(long contentId, CancellationToken cancellationToken = default)
        // WARNING: loads Content bytes — prefer GetAllAsync/ContentDto for UI
        => _db.Contents.AsNoTracking().FirstOrDefaultAsync(x => x.Id == contentId, cancellationToken);

    public async Task<LessonContent> UploadAsync(UploadContentRequest request, CancellationToken cancellationToken = default)
    {
        if (request.FileSize > MaxUploadBytes)
        {
            throw new InvalidOperationException($"حداکثر حجم مجاز ۲۰ مگابایت است. حجم فایل: {request.FileSize / 1024.0 / 1024.0:0.0} MB");
        }

        var folder = await _db.Folders.FirstAsync(x => x.Id == request.FolderId, cancellationToken);
        var ext = Path.GetExtension(request.OriginalFileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(ext))
        {
            ext = ".bin";
        }

        // Read full content into memory (max 20 MB)
        byte[] bytes;
        await using (request.Content)
        {
            using var ms = new MemoryStream((int)Math.Min(request.FileSize, MaxUploadBytes));
            await request.Content.CopyToAsync(ms, cancellationToken);
            bytes = ms.ToArray();
        }

        if (bytes.Length > MaxUploadBytes)
        {
            throw new InvalidOperationException("حداکثر حجم مجاز ۲۰ مگابایت است.");
        }

        var mime = GuessMime(ext);
        var type = request.PreferredType ?? GuessType(ext, mime);

        var entity = new LessonContent
        {
            OriginalFileName = Path.GetFileName(request.OriginalFileName),
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName)
                ? Path.GetFileNameWithoutExtension(request.OriginalFileName)
                : request.DisplayName.Trim(),
            Content = bytes,
            FileExtension = ext,
            MimeType = mime,
            FileSize = bytes.Length,
            Type = type,
            FolderId = folder.Id
        };

        _db.Contents.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task MoveAsync(long contentId, long folderId, CancellationToken cancellationToken = default)
    {
        var content = await _db.Contents.FirstAsync(x => x.Id == contentId, cancellationToken);
        var folder = await _db.Folders.FirstAsync(x => x.Id == folderId, cancellationToken);
        content.FolderId = folder.Id;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(long contentId, CancellationToken cancellationToken = default)
    {
        var content = await _db.Contents.FirstAsync(x => x.Id == contentId, cancellationToken);
        _db.Contents.Remove(content);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<ContentProgressDto?> GetProgressAsync(long userId, long contentId, CancellationToken cancellationToken = default)
    {
        var p = await _db.ContentProgresses
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.ContentId == contentId, cancellationToken);

        if (p is null) return null;

        return new ContentProgressDto(p.ContentId, p.LastPositionSeconds, p.MaxWatchedSeconds, p.ProgressPercent, p.IsCompleted);
    }

    public async Task UpdateProgressAsync(long userId, long contentId, double positionSeconds, double? durationSeconds, CancellationToken cancellationToken = default)
    {
        // Never record progress for Admin users
        var isAdmin = await _db.Users.AsNoTracking()
            .AnyAsync(u => u.Id == userId && u.Role == UserRole.Admin, cancellationToken);
        if (isAdmin)
            return;

        var content = await _db.Contents.AsNoTracking().FirstOrDefaultAsync(x => x.Id == contentId, cancellationToken);
        if (content is null) return;

        var progress = await _db.ContentProgresses
            .FirstOrDefaultAsync(x => x.UserId == userId && x.ContentId == contentId, cancellationToken);

        if (progress is null)
        {
            progress = new ContentProgress
            {
                UserId = userId,
                ContentId = contentId
            };
            _db.ContentProgresses.Add(progress);
        }

        progress.LastPositionSeconds = Math.Max(0, positionSeconds);
        progress.MaxWatchedSeconds = Math.Max(progress.MaxWatchedSeconds, positionSeconds);
        progress.LastWatchedAt = DateTimeOffset.UtcNow;

        var duration = durationSeconds ?? content.DurationSeconds ?? 0;
        if (duration > 0)
        {
            progress.ProgressPercent = Math.Min(100, (progress.MaxWatchedSeconds / duration) * 100);
            if (progress.ProgressPercent >= 90)
            {
                progress.IsCompleted = true;
                progress.ProgressPercent = 100;
            }
        }
        else if (content.Type is ContentType.Document or ContentType.Story or ContentType.Image)
        {
            progress.ProgressPercent = 100;
            progress.IsCompleted = true;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<UserProgressSummaryDto> GetUserProgressSummaryAsync(long userId, CancellationToken cancellationToken = default)
    {
        var accessible = await GetForUserAsync(userId, false, cancellationToken);
        var videoIds = accessible.Where(c => c.Type == ContentType.Video || c.Type == ContentType.Audio).Select(c => c.Id).ToHashSet();
        var storyIds = accessible.Where(c => c.Type == ContentType.Story || c.Type == ContentType.Document).Select(c => c.Id).ToHashSet();

        var progresses = await _db.ContentProgresses
            .AsNoTracking()
            .Where(p => p.UserId == userId && (videoIds.Contains(p.ContentId) || storyIds.Contains(p.ContentId)))
            .ToListAsync(cancellationToken);

        var completedVideos = progresses.Count(p => videoIds.Contains(p.ContentId) && p.IsCompleted);
        var completedStories = progresses.Count(p => storyIds.Contains(p.ContentId) && p.IsCompleted);

        double videoPct = videoIds.Count == 0 ? 0 : (completedVideos * 100.0 / videoIds.Count);
        double storyPct = storyIds.Count == 0 ? 0 : (completedStories * 100.0 / storyIds.Count);

        // Also average partial progress for videos
        if (videoIds.Count > 0)
        {
            var videoProgs = progresses.Where(p => videoIds.Contains(p.ContentId)).ToList();
            if (videoProgs.Count > 0)
            {
                videoPct = videoProgs.Average(p => p.ProgressPercent);
            }
        }

        if (storyIds.Count > 0)
        {
            var storyProgs = progresses.Where(p => storyIds.Contains(p.ContentId)).ToList();
            if (storyProgs.Count > 0)
            {
                storyPct = storyProgs.Average(p => p.ProgressPercent);
            }
        }

        return new UserProgressSummaryDto(
            videoIds.Count,
            completedVideos,
            Math.Round(videoPct, 1),
            storyIds.Count,
            completedStories,
            Math.Round(storyPct, 1));
    }

    private static ContentDto ToDto(LessonContent x) => new(
        x.Id,
        x.OriginalFileName,
        x.DisplayName,
        x.FileExtension,
        x.MimeType,
        x.FileSize,
        x.Type,
        x.DurationSeconds,
        x.FolderId,
        x.Folder?.RelativePath ?? string.Empty);

    private static string GuessMime(string ext) => ext.ToLowerInvariant() switch
    {
        ".mp4" or ".m4v" => "video/mp4",
        ".webm" => "video/webm",
        ".mkv" => "video/x-matroska",
        ".mp3" => "audio/mpeg",
        ".wav" => "audio/wav",
        ".ogg" => "audio/ogg",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".gif" => "image/gif",
        ".svg" => "image/svg+xml",
        ".webp" => "image/webp",
        ".pdf" => "application/pdf",
        ".doc" => "application/msword",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".txt" => "text/plain",
        _ => "application/octet-stream"
    };

    private static ContentType GuessType(string ext, string mime)
    {
        if (mime.StartsWith("video/")) return ContentType.Video;
        if (mime.StartsWith("audio/")) return ContentType.Audio;
        if (mime.StartsWith("image/")) return ContentType.Image;
        if (ext is ".pdf" or ".doc" or ".docx" or ".txt") return ContentType.Document;
        // Treat readable educational docs as Story if desired
        if (ext is ".pdf" or ".docx") return ContentType.Story;
        return ContentType.Other;
    }
}
