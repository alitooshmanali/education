using Lesson.Domain.Entities;
using Lesson.Infrastructure.Abstractions;
using Lesson.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lesson.Infrastructure.Services;

public sealed class FolderService : IFolderService
{
    private readonly LessonDbContext _db;
    public FolderService(LessonDbContext db) => _db = db;

    public async Task<IReadOnlyList<FolderNodeDto>> GetTreeAsync(CancellationToken cancellationToken = default)
    {
        var folders = await _db.Folders
            .Include(x => x.Contents)
            .ToListAsync(cancellationToken);

        var byParent = folders.ToLookup(x => x.ParentFolderId);
        return Build(null, byParent);
    }

    public async Task<Folder> CreateAsync(string name, long? parentFolderId, CancellationToken cancellationToken = default)
    {
        // Pure logical group – no physical directory is created.
        var parentPath = string.Empty;
        if (parentFolderId is not null)
        {
            var parent = await _db.Folders.FirstAsync(x => x.Id == parentFolderId, cancellationToken);
            parentPath = parent.RelativePath;
        }

        var trimmed = name.Trim();
        var relativePath = string.IsNullOrWhiteSpace(parentPath) ? trimmed : $"{parentPath}/{trimmed}";

        // Prevent duplicate names under same parent
        var exists = await _db.Folders.AnyAsync(
            x => x.ParentFolderId == parentFolderId && x.Name == trimmed,
            cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException("گروهی با این نام در همین سطح وجود دارد.");
        }

        var folder = new Folder
        {
            Name = trimmed,
            ParentFolderId = parentFolderId,
            RelativePath = relativePath
        };
        _db.Folders.Add(folder);
        await _db.SaveChangesAsync(cancellationToken);
        return folder;
    }

    public async Task RenameAsync(long folderId, string name, CancellationToken cancellationToken = default)
    {
        var folder = await _db.Folders.FirstAsync(x => x.Id == folderId, cancellationToken);
        folder.Name = name.Trim();
        await RebuildPathAsync(folder, cancellationToken);
    }

    public async Task MoveAsync(long folderId, long? newParentFolderId, CancellationToken cancellationToken = default)
    {
        var folder = await _db.Folders.FirstAsync(x => x.Id == folderId, cancellationToken);
        if (newParentFolderId == folderId)
        {
            throw new InvalidOperationException("نمی‌توان گروه را به خودش منتقل کرد.");
        }
        folder.ParentFolderId = newParentFolderId;
        await RebuildPathAsync(folder, cancellationToken);
    }

    public async Task DeleteAsync(long folderId, CancellationToken cancellationToken = default)
    {
        var hasChildren = await _db.Folders.AnyAsync(x => x.ParentFolderId == folderId, cancellationToken);
        var hasContents = await _db.Contents.AnyAsync(x => x.FolderId == folderId, cancellationToken);
        if (hasChildren || hasContents)
        {
            throw new InvalidOperationException("گروه خالی نیست. ابتدا زیرگروه‌ها و محتوا را حذف کنید.");
        }

        var folder = await _db.Folders.FirstAsync(x => x.Id == folderId, cancellationToken);
        _db.Folders.Remove(folder);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<Folder?> FindByPathAsync(string relativePath, CancellationToken cancellationToken = default)
        => _db.Folders.FirstOrDefaultAsync(x => x.RelativePath == relativePath, cancellationToken);

    private async Task RebuildPathAsync(Folder folder, CancellationToken cancellationToken)
    {
        await _db.Entry(folder).Collection(x => x.ChildFolders).LoadAsync(cancellationToken);

        folder.RelativePath = folder.ParentFolderId is null
            ? folder.Name
            : $"{(await _db.Folders.AsNoTracking().FirstAsync(x => x.Id == folder.ParentFolderId, cancellationToken)).RelativePath}/{folder.Name}";

        foreach (var child in folder.ChildFolders)
        {
            await RebuildDescendantPathsAsync(child, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task RebuildDescendantPathsAsync(Folder child, CancellationToken cancellationToken)
    {
        await _db.Entry(child).Collection(x => x.ChildFolders).LoadAsync(cancellationToken);
        child.RelativePath = child.ParentFolderId is null
            ? child.Name
            : $"{(await _db.Folders.AsNoTracking().FirstAsync(x => x.Id == child.ParentFolderId, cancellationToken)).RelativePath}/{child.Name}";

        foreach (var descendant in child.ChildFolders)
        {
            await RebuildDescendantPathsAsync(descendant, cancellationToken);
        }
    }

    private static IReadOnlyList<FolderNodeDto> Build(long? parentId, ILookup<long?, Folder> lookup)
        => lookup[parentId].Any()
            ? lookup[parentId].Select(f => new FolderNodeDto(
                f.Id,
                f.Name,
                f.RelativePath,
                Build(f.Id, lookup),
                f.Contents.Select(c => new ContentDto(
                    c.Id,
                    c.OriginalFileName,
                    c.DisplayName,
                    c.FileExtension,
                    c.MimeType,
                    c.FileSize,
                    c.Type,
                    c.DurationSeconds,
                    c.FolderId,
                    f.RelativePath)).ToList())).ToList()
            : Array.Empty<FolderNodeDto>();
}
