using Lesson.Domain.Entities;
using Lesson.Infrastructure.Abstractions;
using Lesson.Infrastructure.Options;
using Lesson.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Lesson.Infrastructure.Services;

public sealed class VideoService : IVideoService
{
    private static readonly HashSet<string> DefaultExtensions = [".mkv", ".mp3", ".mp4"];
    private readonly LessonDbContext _db;
    private readonly IFileStorage _storage;
    private readonly LessonStorageOptions _options;

    public VideoService(LessonDbContext db, IFileStorage storage, IOptions<LessonStorageOptions> options)
    {
        _db = db;
        _storage = storage;
        _options = options.Value;
    }

    public Task<IReadOnlyList<VideoDto>> GetAllAsync(CancellationToken cancellationToken = default)
        => _db.Videos.Include(x => x.Folder).OrderBy(x => x.DisplayName).Select(x => new VideoDto(x.Id, x.FileName, x.DisplayName, x.RelativePath, x.FileExtension, x.FileSize, x.FolderId, x.Folder!.RelativePath)).ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<VideoDto>)t.Result, cancellationToken);

    public async Task<IReadOnlyList<VideoDto>> GetForUserAsync(long userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        if (isAdmin)
        {
            return await GetAllAsync(cancellationToken);
        }

        var query = from v in _db.Videos.Include(x => x.Folder)
                    where _db.Permissions.Any(p => p.UserId == userId && p.IsActive && p.RevokedAt == null && (v.FolderId == p.FolderId || _db.Folders.Any(f => f.Id == v.FolderId && f.RelativePath.StartsWith(p.Folder!.RelativePath))))
                    select new VideoDto(v.Id, v.FileName, v.DisplayName, v.RelativePath, v.FileExtension, v.FileSize, v.FolderId, v.Folder!.RelativePath);
        return await query.ToListAsync(cancellationToken);
    }

    public async Task<Video> UploadAsync(UploadVideoRequest request, CancellationToken cancellationToken = default)
    {
        var folder = await _db.Folders.FirstAsync(x => x.Id == request.FolderId, cancellationToken);
        var ext = Path.GetExtension(request.FileName).ToLowerInvariant();
        if (!Allowed(ext))
        {
            throw new InvalidOperationException("Unsupported file type.");
        }

        var safeName = Path.GetFileName(request.FileName);
        var relativePath = string.IsNullOrWhiteSpace(folder.RelativePath) ? safeName : $"{folder.RelativePath}/{safeName}";
        await _storage.SaveAsync(relativePath, request.Content, cancellationToken);
        var video = new Video
        {
            FileName = safeName,
            DisplayName = request.DisplayName.Trim(),
            RelativePath = relativePath,
            StorageKey = relativePath,
            FolderId = folder.Id,
            FileExtension = ext,
            FileSize = request.FileSize
        };
        _db.Videos.Add(video);
        await _db.SaveChangesAsync(cancellationToken);
        return video;
    }

    public async Task MoveAsync(long videoId, long folderId, CancellationToken cancellationToken = default)
    {
        var video = await _db.Videos.Include(x => x.Folder).FirstAsync(x => x.Id == videoId, cancellationToken);
        var folder = await _db.Folders.FirstAsync(x => x.Id == folderId, cancellationToken);
        var source = video.RelativePath;
        video.FolderId = folder.Id;
        video.RelativePath = $"{folder.RelativePath}/{video.FileName}";
        video.StorageKey = video.RelativePath;
        await _storage.MoveAsync(source, video.RelativePath, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(long videoId, CancellationToken cancellationToken = default)
    {
        var video = await _db.Videos.FirstAsync(x => x.Id == videoId, cancellationToken);
        await _storage.DeleteAsync(video.RelativePath, cancellationToken);
        _db.Videos.Remove(video);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<Video?> FindAsync(long videoId, CancellationToken cancellationToken = default)
        => _db.Videos.Include(x => x.Folder).FirstOrDefaultAsync(x => x.Id == videoId, cancellationToken);

    private bool Allowed(string extension)
        => _options.AllowedExtensions.Length == 0 ? DefaultExtensions.Contains(extension) : _options.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
}
