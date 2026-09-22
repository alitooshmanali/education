using Lesson.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lesson.Infrastructure.Abstractions;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Folder> Folders { get; }
    DbSet<Video> Videos { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<UserDevice> UserDevices { get; }
    DbSet<AuthenticationSession> AuthenticationSessions { get; }
    DbSet<VideoWatchSession> VideoWatchSessions { get; }
    DbSet<AuditLog> AuditLogs { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
