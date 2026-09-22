using Lesson.Domain.Common;

namespace Lesson.Domain.Entities;

public sealed class VideoWatchSession : BaseEntity
{
    public long UserId { get; set; }
    public User? User { get; set; }
    public long VideoId { get; set; }
    public Video? Video { get; set; }
    public long UserDeviceId { get; set; }
    public UserDevice? UserDevice { get; set; }
    public long? AuthenticationSessionId { get; set; }
    public AuthenticationSession? AuthenticationSession { get; set; }
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastActivityAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EndedAt { get; set; }
}
