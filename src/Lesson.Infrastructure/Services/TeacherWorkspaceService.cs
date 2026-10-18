using Lesson.Domain.Entities;
using Lesson.Domain.Enums;
using Lesson.Infrastructure.Abstractions;
using Lesson.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lesson.Infrastructure.Services;

public sealed class TeacherWorkspaceService : ITeacherWorkspaceService
{
    private readonly LessonDbContext _db;
    private readonly IClassService _classes;

    public TeacherWorkspaceService(LessonDbContext db, IClassService classes)
    {
        _db = db;
        _classes = classes;
    }

    public async Task EnsureOwnsClassAsync(long teacherId, long classId, CancellationToken ct = default)
    {
        var ok = await _db.ClassRooms.AnyAsync(
            c => c.Id == classId && c.TeacherId == teacherId && c.IsActive, ct);
        if (!ok)
            throw new InvalidOperationException("این کلاس به شما اختصاص ندارد.");
    }

    public async Task<IReadOnlyList<TeacherLevelOption>> GetMyLevelsAsync(long teacherId, CancellationToken ct = default)
    {
        var levels = await _db.ClassRooms.AsNoTracking()
            .Where(c => c.TeacherId == teacherId && c.IsActive && c.Status != ClassStatus.Inactive)
            .GroupBy(c => c.Level)
            .Select(g => new TeacherLevelOption(g.Key, g.Count()))
            .ToListAsync(ct);
        return levels.OrderBy(x => x.Level).ToList();
    }

    public Task<IReadOnlyList<ClassRoomDto>> GetMyClassesAsync(long teacherId, LanguageLevel? level = null, CancellationToken ct = default)
        => level is null
            ? _classes.GetByTeacherAsync(teacherId, ct)
            : GetFiltered(teacherId, level.Value, ct);

    private async Task<IReadOnlyList<ClassRoomDto>> GetFiltered(long teacherId, LanguageLevel level, CancellationToken ct)
    {
        var all = await _classes.GetByTeacherAsync(teacherId, ct);
        return all.Where(c => c.Level == level).ToList();
    }

    public async Task<TeacherDashboardDto?> GetDashboardAsync(long teacherId, long classId, CancellationToken ct = default)
    {
        await EnsureOwnsClassAsync(teacherId, classId, ct);
        var cls = await _db.ClassRooms.AsNoTracking().FirstAsync(c => c.Id == classId, ct);
        var level = cls.Level;

        var studentCount = await _db.Users.CountAsync(
            u => u.Role == UserRole.Student && u.Level == level && u.IsActive, ct);
        var videoCount = await _db.Contents.CountAsync(
            c => c.IsActive && c.Type == ContentType.Video && (c.ClassId == classId || c.Levels.Any(l => l.Level == level)), ct);
        var podcastCount = await _db.Contents.CountAsync(
            c => c.IsActive && c.Type == ContentType.Audio && (c.ClassId == classId || c.Levels.Any(l => l.Level == level)), ct);
        var storyCount = await _db.Stories.CountAsync(
            s => s.IsActive && (s.ClassId == classId || s.Level == level), ct);
        var quizCount = await _db.Quizzes.CountAsync(
            q => q.IsActive && (q.ClassId == classId || q.Level == level), ct);
        var folderCount = await _db.Folders.CountAsync(
            f => f.IsActive && (f.ClassId == classId || f.Level == level), ct);

        return new TeacherDashboardDto(
            cls.Id, cls.Code, cls.Title, cls.Level,
            studentCount, videoCount, podcastCount, storyCount, quizCount, folderCount);
    }

    public async Task<IReadOnlyList<TeacherClassStudentDto>> GetClassStudentsAsync(long teacherId, long classId, CancellationToken ct = default)
    {
        await EnsureOwnsClassAsync(teacherId, classId, ct);
        var cls = await _db.ClassRooms.AsNoTracking().FirstAsync(c => c.Id == classId, ct);
        return await _db.Users.AsNoTracking()
            .Where(u => u.Role == UserRole.Student && u.Level == cls.Level)
            .OrderBy(u => u.Username)
            .Select(u => new TeacherClassStudentDto(u.Id, u.Username, u.Level, u.IsActive))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<FolderNodeDto>> GetClassFoldersAsync(long teacherId, long classId, CancellationToken ct = default)
    {
        await EnsureOwnsClassAsync(teacherId, classId, ct);
        var cls = await _db.ClassRooms.AsNoTracking().FirstAsync(c => c.Id == classId, ct);
        var folders = await _db.Folders.AsNoTracking()
            .Where(f => f.IsActive && (f.ClassId == classId || (f.ClassId == null && f.Level == cls.Level)))
            .OrderBy(f => f.RelativePath)
            .ToListAsync(ct);
        // flat as simple nodes (no nested contents for light load)
        return folders.Where(f => f.ParentFolderId is null)
            .Select(f => ToNode(f, folders))
            .ToList();
    }

    private static FolderNodeDto ToNode(Folder f, List<Folder> all)
    {
        var children = all.Where(x => x.ParentFolderId == f.Id).Select(x => ToNode(x, all)).ToList();
        return new FolderNodeDto(f.Id, f.Name, f.RelativePath, f.Level, children, Array.Empty<ContentDto>());
    }

    public async Task<Folder> CreateClassFolderAsync(long teacherId, long classId, string name, long? parentFolderId, CancellationToken ct = default)
    {
        await EnsureOwnsClassAsync(teacherId, classId, ct);
        var cls = await _db.ClassRooms.AsNoTracking().FirstAsync(c => c.Id == classId, ct);
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("نام پوشه الزامی است.");

        string path = name.Trim();
        if (parentFolderId is long pid)
        {
            var parent = await _db.Folders.FirstAsync(f => f.Id == pid, ct);
            path = string.IsNullOrEmpty(parent.RelativePath) ? name.Trim() : parent.RelativePath + "/" + name.Trim();
        }

        var folder = new Folder
        {
            Name = name.Trim(),
            Level = cls.Level,
            ClassId = classId,
            CreatedByUserId = teacherId,
            ParentFolderId = parentFolderId,
            RelativePath = path
        };
        _db.Folders.Add(folder);
        await _db.SaveChangesAsync(ct);
        return folder;
    }

    public async Task DeleteClassFolderAsync(long teacherId, long folderId, CancellationToken ct = default)
    {
        var folder = await _db.Folders.FirstAsync(f => f.Id == folderId, ct);
        if (folder.ClassId is long cid)
            await EnsureOwnsClassAsync(teacherId, cid, ct);
        else if (folder.CreatedByUserId != teacherId)
            throw new InvalidOperationException("اجازهٔ حذف این پوشه را ندارید.");

        folder.IsActive = false;
        folder.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<TeacherContentListItemDto>> GetClassContentsAsync(
        long teacherId, long classId, ContentType type, CancellationToken ct = default)
    {
        await EnsureOwnsClassAsync(teacherId, classId, ct);
        var list = await _db.Contents.AsNoTracking()
            .Include(c => c.Folder)
            .Where(c => c.IsActive && c.Type == type && c.ClassId == classId)
            .OrderBy(c => c.DisplayName)
            .Select(c => new TeacherContentListItemDto(
                c.Id, c.DisplayName, c.Type, c.FolderId,
                c.Folder != null ? c.Folder.Name : "",
                c.IsRestricted, c.FileSize))
            .ToListAsync(ct);
        return list;
    }

    public async Task SetContentRestrictionAsync(
        long teacherId, long contentId, bool isRestricted, IReadOnlyList<long>? allowedUserIds, CancellationToken ct = default)
    {
        var meta = await _db.Contents.AsNoTracking()
            .Where(c => c.Id == contentId)
            .Select(c => new { c.Id, c.ClassId, c.CreatedByUserId })
            .FirstAsync(ct);
        if (meta.ClassId is long cid)
            await EnsureOwnsClassAsync(teacherId, cid, ct);
        else if (meta.CreatedByUserId != teacherId)
            throw new InvalidOperationException("اجازهٔ تغییر دسترسی این محتوا را ندارید.");

        // Update flags without loading binary Content column
        await _db.Contents.Where(c => c.Id == contentId)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.IsRestricted, isRestricted), ct);

        var oldAccess = await _db.ContentStudentAccesses.Where(a => a.ContentId == contentId).ToListAsync(ct);
        _db.ContentStudentAccesses.RemoveRange(oldAccess);
        if (isRestricted && allowedUserIds is { Count: > 0 })
        {
            foreach (var uid in allowedUserIds.Distinct())
                _db.ContentStudentAccesses.Add(new ContentStudentAccess { ContentId = contentId, UserId = uid });
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<TeacherQuizListItemDto>> GetClassQuizzesAsync(long teacherId, long classId, CancellationToken ct = default)
    {
        await EnsureOwnsClassAsync(teacherId, classId, ct);
        return await _db.Quizzes.AsNoTracking()
            .Where(q => q.IsActive && q.ClassId == classId)
            .OrderByDescending(q => q.Id)
            .Select(q => new TeacherQuizListItemDto(
                q.Id, q.Title, q.Type, q.TimeLimitMinutes, q.TotalScore,
                q.Questions.Count, q.FolderId))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<TeacherStoryListItemDto>> GetClassStoriesAsync(long teacherId, long classId, CancellationToken ct = default)
    {
        await EnsureOwnsClassAsync(teacherId, classId, ct);
        return await _db.Stories.AsNoTracking()
            .Include(s => s.Folder)
            .Where(s => s.IsActive && s.ClassId == classId)
            .OrderByDescending(s => s.Id)
            .Select(s => new TeacherStoryListItemDto(
                s.Id, s.Title, s.FolderId,
                s.Folder != null ? s.Folder.Name : "",
                s.ImageContentId != null, s.VoiceContentId != null))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<TeacherStudentReportDto>> GetClassStudentReportsAsync(
        long teacherId, long classId, CancellationToken ct = default)
    {
        var students = await GetClassStudentsAsync(teacherId, classId, ct);
        var result = new List<TeacherStudentReportDto>();
        foreach (var s in students)
        {
            var videos = await _db.ContentProgresses.CountAsync(
                p => p.UserId == s.UserId && p.IsCompleted && p.ContentId != null, ct);
            var stories = await _db.ContentProgresses.CountAsync(
                p => p.UserId == s.UserId && p.IsCompleted && p.StoryId != null, ct);
            var attempts = await _db.QuizAttempts.AsNoTracking()
                .Where(a => a.UserId == s.UserId && a.IsCompleted)
                .ToListAsync(ct);
            var avgQuiz = attempts.Count == 0 ? (decimal?)null : attempts.Average(a => a.ScoreEarned);
            var sats = await _db.ContentFeedbacks.AsNoTracking()
                .Where(f => f.UserId == s.UserId)
                .Select(f => (double)f.Rating)
                .ToListAsync(ct);
            var avgSat = sats.Count == 0 ? (double?)null : sats.Average();
            result.Add(new TeacherStudentReportDto(
                s.UserId, s.Username, videos, stories, attempts.Count, avgQuiz, avgSat));
        }
        return result;
    }
}
