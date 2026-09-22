using Lesson.Domain.Entities;

namespace Lesson.Infrastructure.Abstractions;

public interface IPermissionService
{
    Task GrantAsync(long userId, long folderId, long? createdByUserId, CancellationToken cancellationToken = default);
    Task RevokeAsync(long permissionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Permission>> GetForUserAsync(long userId, CancellationToken cancellationToken = default);
    Task<bool> HasFolderAccessAsync(long userId, long folderId, CancellationToken cancellationToken = default);
}
