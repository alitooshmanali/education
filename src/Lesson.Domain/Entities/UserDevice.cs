using Lesson.Domain.Common;

namespace Lesson.Domain.Entities;

public sealed class UserDevice : BaseEntity
{
    public long UserId { get; set; }
    public User? User { get; set; }
    public string DeviceIdentifier { get; set; } = string.Empty;
    public DateTimeOffset FirstSeenAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastSeenAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? RevokedAt { get; set; }
    public ICollection<AuthenticationSession> AuthenticationSessions { get; set; } = new List<AuthenticationSession>();
    public ICollection<VideoWatchSession> VideoWatchSessions { get; set; } = new List<VideoWatchSession>();
}
