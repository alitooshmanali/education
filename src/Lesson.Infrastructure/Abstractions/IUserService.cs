using Lesson.Domain.Entities;
using Lesson.Domain.Enums;

namespace Lesson.Infrastructure.Abstractions;

public interface IUserService
{
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> GetByLevelAsync(LanguageLevel level, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> GetTeachersAsync(CancellationToken cancellationToken = default);
    Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<User?> FindByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<User?> FindByIdWithTeacherLevelsAsync(long id, CancellationToken cancellationToken = default);

    Task<User> CreateAsync(
        string username,
        string passwordHash,
        UserRole role,
        LanguageLevel? level = null,
        IReadOnlyList<LanguageLevel>? teacherLevels = null,
        CancellationToken cancellationToken = default);

    Task ResetPasswordAsync(long userId, string passwordHash, CancellationToken cancellationToken = default);
    Task SetRoleAsync(long userId, UserRole role, CancellationToken cancellationToken = default);
    Task SetLevelAsync(long userId, LanguageLevel? level, CancellationToken cancellationToken = default);
    Task SetTeacherLevelsAsync(long userId, IReadOnlyList<LanguageLevel> levels, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LanguageLevel>> GetTeacherLevelsAsync(long userId, CancellationToken cancellationToken = default);
    Task SetActiveAsync(long userId, bool isActive, CancellationToken cancellationToken = default);
}
