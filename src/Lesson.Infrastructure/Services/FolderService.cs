using Lesson.Domain;
using Lesson.Domain.Entities;
using Lesson.Domain.Enums;
using Lesson.Infrastructure.Abstractions;
using Lesson.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lesson.Infrastructure.Services;

public sealed class FolderService : IFolderService
{
    private readonly LessonDbContext _db;
    public FolderService(LessonDbContext db) => _db = db;

    public async Task<IReadOnlyList<FolderNodeDto>> GetTreeAsync(LanguageLevel? levelFilter = null, CancellationToken cancellationToken = default)
    {
        var query = _db.Folders.AsNoTracking().AsQueryable();
        if (levelFilter.HasValue)
            query = query.Where(f => f.Level == levelFilter.Value);

        var folders = await query.ToListAsync(cancellationToken);

        var folderIds = folders.Select(f => f.Id).ToList();

        var contents = await _db.Contents.AsNoTracking()
            .Include(c => c.Levels)
            .Where(c => folderIds.Contains(c.FolderId))
            .ToListAsync(cancellationToken);

        var contentsByFolder = contents.ToLookup(c => c.FolderId);
        var pathById = folders.ToDictionary(f => f.Id, f => f.RelativePath);
        var byParent = folders.ToLookup(x => x.ParentFolderId);

        return Build(null, byParent, contentsByFolder, pathById);
    }

    public async Task<IReadOnlyList<Folder>> GetFlatAsync(LanguageLevel? levelFilter = null, CancellationToken cancellationToken = default)
    {
        var query = _db.Folders.AsNoTracking().AsQueryable();
        if (levelFilter.HasValue)
            query = query.Where(f => f.Level == levelFilter.Value);
        return await query.OrderBy(f => f.RelativePath).ToListAsync(cancellationToken);
    }

    public async Task<Folder> CreateAsync(string name, long? parentFolderId, LanguageLevel level, CancellationToken cancellationToken = default)
    {
        var parentPath = string.Empty;
        if (parentFolderId is not null)
        {
            var parent = await _db.Folders.FirstAsync(x => x.Id == parentFolderId, cancellationToken);
            if (parent.Level != level)
                throw new InvalidOperationException("پوشه والد باید در همان سطح زبانی باشد.");
            parentPath = parent.RelativePath;
        }

        var trimmed = name.Trim();
        if (SystemFolders.IsProtected(trimmed))
            throw new InvalidOperationException("این نام برای پوشهٔ سیستمی رزرو شده و قابل ایجاد دستی نیست.");

        var relativePath = string.IsNullOrWhiteSpace(parentPath) ? trimmed : $"{parentPath}/{trimmed}";

        var exists = await _db.Folders.AnyAsync(
            x => x.ParentFolderId == parentFolderId && x.Name == trimmed && x.Level == level,
            cancellationToken);
        if (exists)
            throw new InvalidOperationException("گروهی با این نام در همین سطح وجود دارد.");

        var folder = new Folder
        {
            Name = trimmed,
            ParentFolderId = parentFolderId,
            RelativePath = relativePath,
            Level = level
        };
        _db.Folders.Add(folder);
        await _db.SaveChangesAsync(cancellationToken);
        return folder;
    }

    public async Task RenameAsync(long folderId, string name, CancellationToken cancellationToken = default)
    {
        var folder = await _db.Folders.FirstAsync(x => x.Id == folderId, cancellationToken);
        if (SystemFolders.IsProtected(folder.Name))
            throw new InvalidOperationException("پوشهٔ سیستمی قابل تغییر نام نیست.");
        var trimmed = name.Trim();
        if (SystemFolders.IsProtected(trimmed))
            throw new InvalidOperationException("این نام برای پوشهٔ سیستمی رزرو شده است.");
        folder.Name = trimmed;
        await RebuildPathAsync(folder, cancellationToken);
    }

    public async Task DeleteAsync(long folderId, CancellationToken cancellationToken = default)
    {
        var folder = await _db.Folders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == folderId, cancellationToken);
        if (folder is not null && SystemFolders.IsProtected(folder.Name))
            throw new InvalidOperationException("پوشهٔ سیستمی قابل حذف نیست.");

        var hasChildren = await _db.Folders.AnyAsync(x => x.ParentFolderId == folderId, cancellationToken);
        if (hasChildren)
            throw new InvalidOperationException("ابتدا زیرپوشه‌ها را حذف کنید.");

        var hasContent = await _db.Contents.AnyAsync(x => x.FolderId == folderId, cancellationToken);
        if (hasContent)
            throw new InvalidOperationException("ابتدا محتواهای این پوشه را حذف یا منتقل کنید.");

        var hasStories = await _db.Stories.AnyAsync(x => x.FolderId == folderId, cancellationToken);
        if (hasStories)
            throw new InvalidOperationException("ابتدا داستان‌های این پوشه را حذف کنید.");

        var entity = await _db.Folders.FirstAsync(x => x.Id == folderId, cancellationToken);
        _db.Folders.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<Folder?> FindAsync(long folderId, CancellationToken cancellationToken = default)
        => _db.Folders.FirstOrDefaultAsync(x => x.Id == folderId, cancellationToken);

    private async Task RebuildPathAsync(Folder folder, CancellationToken cancellationToken)
    {
        await _db.Entry(folder).Collection(x => x.ChildFolders).LoadAsync(cancellationToken);

        folder.RelativePath = folder.ParentFolderId is null
            ? folder.Name
            : $"{(await _db.Folders.AsNoTracking().FirstAsync(x => x.Id == folder.ParentFolderId, cancellationToken)).RelativePath}/{folder.Name}";

        foreach (var child in folder.ChildFolders)
            await RebuildDescendantPathsAsync(child, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task RebuildDescendantPathsAsync(Folder child, CancellationToken cancellationToken)
    {
        await _db.Entry(child).Collection(x => x.ChildFolders).LoadAsync(cancellationToken);
        child.RelativePath = child.ParentFolderId is null
            ? child.Name
            : $"{(await _db.Folders.AsNoTracking().FirstAsync(x => x.Id == child.ParentFolderId, cancellationToken)).RelativePath}/{child.Name}";

        foreach (var descendant in child.ChildFolders)
            await RebuildDescendantPathsAsync(descendant, cancellationToken);
    }

    private static IReadOnlyList<FolderNodeDto> Build(
        long? parentId,
        ILookup<long?, Folder> lookup,
        ILookup<long, LessonContent> contentsByFolder,
        IReadOnlyDictionary<long, string> pathById)
    {
        if (!lookup[parentId].Any())
            return Array.Empty<FolderNodeDto>();

        return lookup[parentId].Select(f =>
        {
            var path = pathById.TryGetValue(f.Id, out var p) ? p : f.RelativePath;
            var contents = contentsByFolder[f.Id]
                .Select(c => new ContentDto(
                    c.Id,
                    c.OriginalFileName,
                    c.DisplayName,
                    c.FileExtension,
                    c.MimeType,
                    c.FileSize,
                    c.Type,
                    c.DurationSeconds,
                    c.FolderId,
                    path,
                    c.Levels?.Select(l => l.Level).ToList() ?? new List<LanguageLevel>()))
                .ToList();

            return new FolderNodeDto(
                f.Id,
                f.Name,
                f.RelativePath,
                f.Level,
                Build(f.Id, lookup, contentsByFolder, pathById),
                contents);
        }).ToList();
    }
    public bool IsSystemFolder(string? name) => SystemFolders.IsProtected(name);

    public async Task EnsureSystemFoldersAsync(CancellationToken cancellationToken = default)
    {
        foreach (var name in SystemFolders.All)
        {
            var exists = await _db.Folders.AnyAsync(
                x => x.Name == name && x.ParentFolderId == null,
                cancellationToken);
            if (exists) continue;

            _db.Folders.Add(new Folder
            {
                Name = name,
                ParentFolderId = null,
                RelativePath = name,
                Level = LanguageLevel.A1,
                IsActive = true
            });
        }
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<Folder> GetSystemFolderAsync(string systemFolderName, CancellationToken cancellationToken = default)
    {
        if (!SystemFolders.IsProtected(systemFolderName))
            throw new ArgumentException("نام پوشهٔ سیستمی معتبر نیست.", nameof(systemFolderName));

        await EnsureSystemFoldersAsync(cancellationToken);
        var folder = await _db.Folders.FirstOrDefaultAsync(
            x => x.Name == systemFolderName && x.ParentFolderId == null,
            cancellationToken);
        if (folder is null)
            throw new InvalidOperationException($"پوشهٔ سیستمی {systemFolderName} یافت نشد.");
        return folder;
    }

}
