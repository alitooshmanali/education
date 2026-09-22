using Lesson.Domain.Common;

namespace Lesson.Domain.Entities;

public sealed class AuthenticationSession : BaseEntity
{
    public long UserId { get; set; }
    public User? User { get; set; }
    public long UserDeviceId { get; set; }
    public UserDevice? UserDevice { get; set; }
    public string SessionKey { get; set; } = string.Empty;
    public DateTimeOffset LastSeenAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? RevokedAt { get; set; }
}
