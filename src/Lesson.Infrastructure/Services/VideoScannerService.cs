using Lesson.Domain.Entities;
using Lesson.Infrastructure.Abstractions;
using Lesson.Infrastructure.Options;
using Lesson.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Lesson.Infrastructure.Services;

public sealed class VideoScannerService : IVideoScannerService
{
    private readonly LessonDbContext _db;
    private readonly IFileStorage _storage;
    private readonly LessonStorageOptions _options;

    public VideoScannerService(LessonDbContext db, IFileStorage storage, IOptions<LessonStorageOptions> options)
    {
        _db = db;
        _storage = storage;
        _options = options.Value;
    }

    public async Task SyncAsync(CancellationToken cancellationToken = default)
    {
        var root = Path.GetFullPath(_options.RootPath);
        if (!Directory.Exists(root))
        {
            return;
        }

        var seenFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenVideos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
            var dir = Path.GetDirectoryName(relative)?.Replace('\\', '/') ?? string.Empty;
            var parts = dir.Split('/', StringSplitOptions.RemoveEmptyEntries);
            string currentPath = string.Empty;
            long? parentId = null;
            foreach (var part in parts)
            {
                currentPath = string.IsNullOrWhiteSpace(currentPath) ? part : $"{currentPath}/{part}";
                seenFolders.Add(currentPath);
                var folder = await _db.Folders.FirstOrDefaultAsync(x => x.RelativePath == currentPath, cancellationToken);
                if (folder is null)
                {
                    folder = new Folder { Name = part, RelativePath = currentPath, ParentFolderId = parentId };
                    _db.Folders.Add(folder);
                    await _db.SaveChangesAsync(cancellationToken);
                }
                parentId = folder.Id;
            }

            var ext = Path.GetExtension(file).ToLowerInvariant();
            if (_options.AllowedExtensions.Length > 0 && !_options.AllowedExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            seenVideos.Add(relative);
            var existing = await _db.Videos.FirstOrDefaultAsync(x => x.RelativePath == relative, cancellationToken);
            if (existing is null)
            {
                var info = new FileInfo(file);
                _db.Videos.Add(new Video
                {
                    FileName = Path.GetFileName(file),
                    DisplayName = Path.GetFileNameWithoutExtension(file),
                    RelativePath = relative,
                    StorageKey = relative,
                    FolderId = parentId ?? 0,
                    FileExtension = ext,
                    FileSize = info.Length
                });
                await _db.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
