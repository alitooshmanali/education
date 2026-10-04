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

    public ContentService(LessonDbContext db, IOptions<LessonStorageOptions> options)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ContentDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = await _db.Contents.AsNoTracking()
            .Include(x => x.Folder)
            .Include(x => x.Levels)
            .OrderBy(x => x.DisplayName)
            .ToListAsync(cancellationToken);

        return list.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<ContentDto>> GetByLevelAsync(LanguageLevel level, CancellationToken cancellationToken = default)
    {
        var list = await _db.Contents.AsNoTracking()
            .Include(x => x.Folder)
            .Include(x => x.Levels)
            .Where(x => x.Levels.Any(l => l.Level == level) || x.Folder!.Level == level)
            .OrderBy(x => x.DisplayName)
            .ToListAsync(cancellationToken);

        return list.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<ContentDto>> GetForUserAsync(long userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        if (isAdmin)
            return await GetAllAsync(cancellationToken);

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user?.Level is null)
            return Array.Empty<ContentDto>();

        var level = user.Level.Value;

        // Content visible if assigned to student's level (via ContentLevels or folder level)
        var list = await _db.Contents.AsNoTracking()
            .Include(x => x.Folder)
            .Include(x => x.Levels)
            .Where(c => c.IsActive && (
                c.Levels.Any(l => l.Level == level) ||
                (c.Folder != null && c.Folder.Level == level)))
            .OrderBy(x => x.DisplayName)
            .ToListAsync(cancellationToken);

        return list.Select(ToDto).ToList();
    }

    public Task<LessonContent?> FindAsync(long contentId, CancellationToken cancellationToken = default)
        => _db.Contents.Include(x => x.Levels).Include(x => x.Folder)
            .FirstOrDefaultAsync(x => x.Id == contentId, cancellationToken);

    public async Task<IReadOnlyList<LanguageLevel>> GetAllowedUploadLevelsAsync(long userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        if (isAdmin)
            return Enum.GetValues<LanguageLevel>().ToList();

        return await _db.TeacherLevels.AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.Level)
            .ToListAsync(cancellationToken);
    }

    private async Task EnsureTeacherLevelsAllowedAsync(
        long? actorUserId,
        bool isAdmin,
        IReadOnlyList<LanguageLevel> levels,
        CancellationToken cancellationToken)
    {
        if (isAdmin || actorUserId is null)
            return;

        var allowed = await _db.TeacherLevels.AsNoTracking()
            .Where(x => x.UserId == actorUserId.Value)
            .Select(x => x.Level)
            .ToListAsync(cancellationToken);

        if (allowed.Count == 0)
            throw new InvalidOperationException("هیچ سطح زبانی برای این مدرس تعریف نشده است.");

        var forbidden = levels.Distinct().Where(l => !allowed.Contains(l)).ToList();
        if (forbidden.Count > 0)
            throw new InvalidOperationException(
                "مدرس فقط می‌تواند محتوای مربوط به سطوح مجاز خود را بارگذاری کند. سطوح غیرمجاز: " +
                string.Join(", ", forbidden));
    }

    public async Task<LessonContent> UploadAsync(
        UploadContentRequest request,
        long? actorUserId = null,
        bool isAdmin = true,
        CancellationToken cancellationToken = default)
    {
        if (request.Levels is null || request.Levels.Count == 0)
            throw new InvalidOperationException("حداقل یک سطح زبانی باید انتخاب شود.");

        await EnsureTeacherLevelsAllowedAsync(actorUserId, isAdmin, request.Levels, cancellationToken);

        var folder = await _db.Folders.FirstAsync(x => x.Id == request.FolderId, cancellationToken);
        var ext = Path.GetExtension(request.OriginalFileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(ext))
            ext = ".bin";

        byte[] bytes;
        await using (request.Content)
        {
            using var ms = new MemoryStream((int)Math.Min(request.FileSize, MaxUploadBytes));
            await request.Content.CopyToAsync(ms, cancellationToken);
            bytes = ms.ToArray();
        }

        if (bytes.Length > MaxUploadBytes)
            throw new InvalidOperationException("حداکثر حجم مجاز ۲۰ مگابایت است.");

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

        foreach (var level in request.Levels.Distinct())
        {
            entity.Levels.Add(new ContentLevel { Level = level });
        }

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

    public async Task SetLevelsAsync(
        long contentId,
        IReadOnlyList<LanguageLevel> levels,
        long? actorUserId = null,
        bool isAdmin = true,
        CancellationToken cancellationToken = default)
    {
        if (levels is null || levels.Count == 0)
            throw new InvalidOperationException("حداقل یک سطح زبانی باید انتخاب شود.");

        await EnsureTeacherLevelsAllowedAsync(actorUserId, isAdmin, levels, cancellationToken);

        var content = await _db.Contents.Include(x => x.Levels).FirstAsync(x => x.Id == contentId, cancellationToken);
        content.Levels.Clear();
        foreach (var level in levels.Distinct())
            content.Levels.Add(new ContentLevel { ContentId = contentId, Level = level });

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
        var p = await _db.ContentProgresses.AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.ContentId == contentId, cancellationToken);
        if (p is null) return null;
        return new ContentProgressDto(p.ContentId, p.StoryId, p.LastPositionSeconds, p.MaxWatchedSeconds, p.ProgressPercent, p.IsCompleted);
    }

    public async Task UpdateProgressAsync(long userId, long contentId, double positionSeconds, double? durationSeconds, CancellationToken cancellationToken = default)
    {
        var p = await _db.ContentProgresses
            .FirstOrDefaultAsync(x => x.UserId == userId && x.ContentId == contentId, cancellationToken);

        if (p is null)
        {
            p = new ContentProgress { UserId = userId, ContentId = contentId };
            _db.ContentProgresses.Add(p);
        }

        p.LastPositionSeconds = Math.Max(0, positionSeconds);
        p.MaxWatchedSeconds = Math.Max(p.MaxWatchedSeconds, positionSeconds);
        p.LastWatchedAt = DateTimeOffset.UtcNow;

        if (durationSeconds is > 0)
        {
            p.ProgressPercent = Math.Min(100, Math.Round(p.MaxWatchedSeconds / durationSeconds.Value * 100, 1));
            if (p.ProgressPercent >= 90)
                p.IsCompleted = true;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<UserProgressSummaryDto> GetUserProgressSummaryAsync(long userId, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user?.Level is null)
            return new UserProgressSummaryDto(0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

        var level = user.Level.Value;

        var videos = await _db.Contents.AsNoTracking()
            .Where(c => c.Type == ContentType.Video && c.IsActive &&
                (c.Levels.Any(l => l.Level == level) || c.Folder!.Level == level))
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        var stories = await _db.Stories.AsNoTracking()
            .Where(s => s.IsActive && s.Level == level)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        var quizzes = await _db.Quizzes.AsNoTracking()
            .Where(q => q.IsActive && q.Level == level &&
                (q.IsLevelWide || q.Type == QuizType.General ||
                 q.Assignments.Any(a => a.UserId == userId)))
            .Select(q => q.Id)
            .ToListAsync(cancellationToken);

        var progresses = await _db.ContentProgresses.AsNoTracking()
            .Where(p => p.UserId == userId)
            .ToListAsync(cancellationToken);

        var completedVideos = progresses.Count(p => p.ContentId.HasValue && videos.Contains(p.ContentId.Value) && p.IsCompleted);
        var completedStories = progresses.Count(p => p.StoryId.HasValue && stories.Contains(p.StoryId.Value) && p.IsCompleted);

        var completedQuizzes = await _db.QuizAttempts.AsNoTracking()
            .CountAsync(a => a.UserId == userId && a.IsCompleted && quizzes.Contains(a.QuizId), cancellationToken);

        double videoPct = videos.Count == 0 ? 100 : (completedVideos * 100.0 / videos.Count);
        double storyPct = stories.Count == 0 ? 100 : (completedStories * 100.0 / stories.Count);
        double quizPct = quizzes.Count == 0 ? 100 : (completedQuizzes * 100.0 / quizzes.Count);

        // Average partial video progress if available
        if (videos.Count > 0)
        {
            var vp = progresses.Where(p => p.ContentId.HasValue && videos.Contains(p.ContentId.Value)).ToList();
            if (vp.Count > 0)
                videoPct = vp.Average(p => p.ProgressPercent);
        }

        var parts = new List<double>();
        if (videos.Count > 0) parts.Add(videoPct);
        if (stories.Count > 0) parts.Add(storyPct);
        if (quizzes.Count > 0) parts.Add(quizPct);
        double overall = parts.Count == 0 ? 0 : Math.Round(parts.Average(), 1);

        return new UserProgressSummaryDto(
            videos.Count, completedVideos, Math.Round(videoPct, 1),
            stories.Count, completedStories, Math.Round(storyPct, 1),
            quizzes.Count, completedQuizzes, Math.Round(quizPct, 1),
            overall);
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
        x.Folder?.RelativePath ?? string.Empty,
        x.Levels?.Select(l => l.Level).ToList() ?? new List<LanguageLevel>());

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
        return ContentType.Other;
    }
}
