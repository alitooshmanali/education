using Lesson.Domain.Entities;
using Lesson.Domain.Enums;
using Lesson.Infrastructure.Abstractions;
using Lesson.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lesson.Infrastructure.Services;

public sealed class UserService : IUserService
{
    private readonly LessonDbContext _db;
    public UserService(LessonDbContext db) => _db = db;

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _db.Users.OrderBy(x => x.Username).ToListAsync(cancellationToken);

    public Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default)
        => _db.Users.Include(x => x.Devices).FirstOrDefaultAsync(x => x.Username == username, cancellationToken);

    public Task<User?> FindByIdAsync(long id, CancellationToken cancellationToken = default)
        => _db.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<User> CreateAsync(string username, string passwordHash, UserRole role, CancellationToken cancellationToken = default)
    {
        var user = new User { Username = username.Trim(), PasswordHash = passwordHash, Role = role };
        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task ResetPasswordAsync(long userId, string passwordHash, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstAsync(x => x.Id == userId, cancellationToken);
        user.PasswordHash = passwordHash;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SetRoleAsync(long userId, UserRole role, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstAsync(x => x.Id == userId, cancellationToken);
        user.Role = role;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SetActiveAsync(long userId, bool isActive, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstAsync(x => x.Id == userId, cancellationToken);
        user.IsActive = isActive;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
