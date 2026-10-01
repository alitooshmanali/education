using Lesson.Domain.Entities;
using Lesson.Domain.Enums;
using Lesson.Infrastructure.Abstractions;
using Lesson.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lesson.Infrastructure.Services;

public sealed class StoryService : IStoryService
{
    private readonly LessonDbContext _db;
    public StoryService(LessonDbContext db) => _db = db;

    public async Task<IReadOnlyList<StoryDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = await _db.Stories.AsNoTracking()
            .Include(s => s.Folder)
            .OrderBy(s => s.Title)
            .ToListAsync(cancellationToken);
        return list.Select(s => ToDto(s, false)).ToList();
    }

    public async Task<IReadOnlyList<StoryDto>> GetByLevelAsync(LanguageLevel level, CancellationToken cancellationToken = default)
    {
        var list = await _db.Stories.AsNoTracking()
            .Include(s => s.Folder)
            .Where(s => s.Level == level && s.IsActive)
            .OrderBy(s => s.Title)
            .ToListAsync(cancellationToken);
        return list.Select(s => ToDto(s, false)).ToList();
    }

    public async Task<IReadOnlyList<StoryDto>> GetForUserAsync(long userId, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user?.Level is null) return Array.Empty<StoryDto>();

        var level = user.Level.Value;
        var list = await _db.Stories.AsNoTracking()
            .Include(s => s.Folder)
            .Where(s => s.Level == level && s.IsActive)
            .OrderBy(s => s.Title)
            .ToListAsync(cancellationToken);

        var completedIds = await _db.ContentProgresses.AsNoTracking()
            .Where(p => p.UserId == userId && p.StoryId != null && p.IsCompleted)
            .Select(p => p.StoryId!.Value)
            .ToListAsync(cancellationToken);

        return list.Select(s => ToDto(s, completedIds.Contains(s.Id))).ToList();
    }

    public Task<Story?> FindAsync(long id, CancellationToken cancellationToken = default)
        => _db.Stories.Include(s => s.Folder).FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<Story> CreateAsync(CreateStoryRequest request, CancellationToken cancellationToken = default)
    {
        var folder = await _db.Folders.FirstAsync(f => f.Id == request.FolderId, cancellationToken);
        if (folder.Level != request.Level)
            throw new InvalidOperationException("پوشه باید متعلق به همان سطح زبانی باشد.");

        var story = new Story
        {
            Title = request.Title.Trim(),
            Text = request.Text,
            FolderId = request.FolderId,
            Level = request.Level,
            ImageContentId = request.ImageContentId,
            VoiceContentId = request.VoiceContentId
        };
        _db.Stories.Add(story);
        await _db.SaveChangesAsync(cancellationToken);
        return story;
    }

    public async Task UpdateAsync(long id, CreateStoryRequest request, CancellationToken cancellationToken = default)
    {
        var story = await _db.Stories.FirstAsync(s => s.Id == id, cancellationToken);
        var folder = await _db.Folders.FirstAsync(f => f.Id == request.FolderId, cancellationToken);
        if (folder.Level != request.Level)
            throw new InvalidOperationException("پوشه باید متعلق به همان سطح زبانی باشد.");

        story.Title = request.Title.Trim();
        story.Text = request.Text;
        story.FolderId = request.FolderId;
        story.Level = request.Level;
        story.ImageContentId = request.ImageContentId;
        story.VoiceContentId = request.VoiceContentId;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var story = await _db.Stories.FirstAsync(s => s.Id == id, cancellationToken);
        _db.Stories.Remove(story);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkProgressAsync(long userId, long storyId, double progressPercent, bool completed, CancellationToken cancellationToken = default)
    {
        var p = await _db.ContentProgresses
            .FirstOrDefaultAsync(x => x.UserId == userId && x.StoryId == storyId, cancellationToken);

        if (p is null)
        {
            p = new ContentProgress { UserId = userId, StoryId = storyId };
            _db.ContentProgresses.Add(p);
        }

        p.ProgressPercent = Math.Clamp(progressPercent, 0, 100);
        p.IsCompleted = completed || p.ProgressPercent >= 90;
        p.LastWatchedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static StoryDto ToDto(Story s, bool completed) => new(
        s.Id, s.Title, s.Text, s.ImageContentId, s.VoiceContentId,
        s.FolderId, s.Folder?.RelativePath ?? "", s.Level, completed);
}
