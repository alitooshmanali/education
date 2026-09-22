using Lesson.Domain.Entities;
using Lesson.Infrastructure.Abstractions;
using Lesson.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lesson.Infrastructure.Services;

public sealed class AuditService : IAuditService
{
    private readonly LessonDbContext _db;

    public AuditService(LessonDbContext db) => _db = db;

    public async Task WriteAsync(long? userId, string action, string entityType, long? entityId, string description, CancellationToken cancellationToken = default)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Description = description
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Domain.Entities.AuditLog>> GetRecentAsync(int take = 100, CancellationToken cancellationToken = default)
    {
        return await _db.AuditLogs.OrderByDescending(x => x.CreatedAt).Take(take).ToListAsync(cancellationToken);
    }
}
