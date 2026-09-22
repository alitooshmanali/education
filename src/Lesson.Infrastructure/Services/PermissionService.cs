using Lesson.Domain.Entities;
using Lesson.Infrastructure.Abstractions;
using Lesson.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lesson.Infrastructure.Services;

public sealed class PermissionService : IPermissionService
{
    private readonly LessonDbContext _db;
    public PermissionService(LessonDbContext db) => _db = db;

    public async Task GrantAsync(long userId, long folderId, long? createdByUserId, CancellationToken cancellationToken = default)
    {
        var existing = await _db.Permissions.FirstOrDefaultAsync(x => x.UserId == userId && x.FolderId == folderId, cancellationToken);
        if (existing is null)
        {
            _db.Permissions.Add(new Permission { UserId = userId, FolderId = folderId, CreatedByUserId = createdByUserId });
        }
        else
        {
            existing.IsActive = true;
            existing.RevokedAt = null;
            existing.CreatedByUserId = createdByUserId;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeAsync(long permissionId, CancellationToken cancellationToken = default)
    {
        var permission = await _db.Permissions.FirstAsync(x => x.Id == permissionId, cancellationToken);
        permission.IsActive = false;
        permission.RevokedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Permission>> GetForUserAsync(long userId, CancellationToken cancellationToken = default)
        => await _db.Permissions.Include(x => x.Folder).Where(x => x.UserId == userId && x.IsActive && x.RevokedAt == null).ToListAsync(cancellationToken);

    public async Task<bool> HasFolderAccessAsync(long userId, long folderId, CancellationToken cancellationToken = default)
    {
        var current = await _db.Folders.FirstAsync(x => x.Id == folderId, cancellationToken);
        while (true)
        {
            var allowed = await _db.Permissions.AnyAsync(x => x.UserId == userId && x.FolderId == current.Id && x.IsActive && x.RevokedAt == null, cancellationToken);
            if (allowed)
            {
                return true;
            }

            if (current.ParentFolderId is null)
            {
                return false;
            }

            current = await _db.Folders.FirstAsync(x => x.Id == current.ParentFolderId, cancellationToken);
        }
    }
}
