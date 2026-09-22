using Lesson.Domain.Common;

namespace Lesson.Domain.Entities;

public sealed class AuditLog : BaseEntity
{
    public long? UserId { get; set; }
    public User? User { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public long? EntityId { get; set; }
    public string Description { get; set; } = string.Empty;
}
