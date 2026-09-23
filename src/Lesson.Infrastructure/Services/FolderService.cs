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

    private sealed record ContentMeta(
        long Id,
        string OriginalFileName,
        string DisplayName,
        string FileExtension,
        string MimeType,
        long FileSize,
        ContentType Type,
        double? DurationSeconds,
        long FolderId);

    public async Task<IReadOnlyList<FolderNodeDto>> GetTreeAsync(CancellationToken cancellationToken = default)
    {
        var folders = await _db.Folders.AsNoTracking().ToListAsync(cancellationToken);

        // Metadata only — never the Content varbinary column
        var contentMeta = await _db.Contents.AsNoTracking()
            .Select(c => new ContentMeta(
                c.Id,
                c.OriginalFileName,
                c.DisplayName,
                c.FileExtension,
                c.MimeType,
                c.FileSize,
                c.Type,
                c.DurationSeconds,
                c.FolderId))
            .ToListAsync(cancellationToken);

        var contentsByFolder = contentMeta.ToLookup(c => c.FolderId);
        var pathById = folders.ToDictionary(f => f.Id, f => f.RelativePath);
        var byParent = folders.ToLookup(x => x.ParentFolderId);

        return Build(null, byParent, contentsByFolder, pathById);
    }

    public async Task<Folder> CreateAsync(string name, long? parentFolderId, CancellationToken cancellationToken = default)
    {
        var parentPath = string.Empty;
        if (parentFolderId is not null)
        {
            var parent = await _db.Folders.FirstAsync(x => x.Id == parentFolderId, cancellationToken);
            parentPath = parent.RelativePath;
        }

        var trimmed = name.Trim();
        var relativePath = string.IsNullOrWhiteSpace(parentPath) ? trimmed : $"{parentPath}/{trimmed}";

        var exists = await _db.Folders.AnyAsync(
            x => x.ParentFolderId == parentFolderId && x.Name == trimmed,
            cancellationToken);
        if (exists)
            throw new InvalidOperationException("گروهی با این نام در همین سطح وجود دارد.");

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
            throw new InvalidOperationException("نمی‌توان گروه را به خودش منتقل کرد.");

        folder.ParentFolderId = newParentFolderId;
        await RebuildPathAsync(folder, cancellationToken);
    }

    public async Task DeleteAsync(long folderId, CancellationToken cancellationToken = default)
    {
        var hasChildren = await _db.Folders.AnyAsync(x => x.ParentFolderId == folderId, cancellationToken);
        var hasContents = await _db.Contents.AnyAsync(x => x.FolderId == folderId, cancellationToken);
        if (hasChildren || hasContents)
            throw new InvalidOperationException("گروه خالی نیست. ابتدا زیرگروه‌ها و محتوا را حذف کنید.");

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
        ILookup<long, ContentMeta> contentsByFolder,
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
                    path))
                .ToList();

            return new FolderNodeDto(
                f.Id,
                f.Name,
                f.RelativePath,
                Build(f.Id, lookup, contentsByFolder, pathById),
                contents);
        }).ToList();
    }
}
