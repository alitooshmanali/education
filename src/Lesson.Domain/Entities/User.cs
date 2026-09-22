using Lesson.Domain.Common;
using Lesson.Domain.Enums;

namespace Lesson.Domain.Entities;

public sealed class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public ICollection<Permission> Permissions { get; set; } = new List<Permission>();
    public ICollection<UserDevice> Devices { get; set; } = new List<UserDevice>();
    public ICollection<AuthenticationSession> AuthenticationSessions { get; set; } = new List<AuthenticationSession>();
    public ICollection<VideoWatchSession> VideoWatchSessions { get; set; } = new List<VideoWatchSession>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
