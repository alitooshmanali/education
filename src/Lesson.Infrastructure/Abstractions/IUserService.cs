using Lesson.Domain.Entities;
using Lesson.Domain.Enums;

namespace Lesson.Infrastructure.Abstractions;

public interface IUserService
{
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<User?> FindByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<User> CreateAsync(string username, string passwordHash, UserRole role, CancellationToken cancellationToken = default);
    Task ResetPasswordAsync(long userId, string passwordHash, CancellationToken cancellationToken = default);
    Task SetRoleAsync(long userId, UserRole role, CancellationToken cancellationToken = default);
    Task SetActiveAsync(long userId, bool isActive, CancellationToken cancellationToken = default);
}
