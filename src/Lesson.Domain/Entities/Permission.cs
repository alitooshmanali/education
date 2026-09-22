using Lesson.Domain.Common;
using Lesson.Domain.Enums;

namespace Lesson.Domain.Entities;

public sealed class Permission : BaseEntity
{
    public long UserId { get; set; }
    public User? User { get; set; }
    public long FolderId { get; set; }
    public Folder? Folder { get; set; }
    public PermissionType PermissionType { get; set; } = PermissionType.Grant;
    public long? CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}
