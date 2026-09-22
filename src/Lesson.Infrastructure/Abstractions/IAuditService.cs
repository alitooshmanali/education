namespace Lesson.Infrastructure.Abstractions;

public interface IAuditService
{
    Task WriteAsync(long? userId, string action, string entityType, long? entityId, string description, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Domain.Entities.AuditLog>> GetRecentAsync(int take = 100, CancellationToken cancellationToken = default);
}
