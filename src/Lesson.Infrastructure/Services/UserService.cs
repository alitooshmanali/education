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
        => await _db.Users.AsNoTracking().OrderBy(x => x.Username).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<User>> GetByLevelAsync(LanguageLevel level, CancellationToken cancellationToken = default)
        => await _db.Users.AsNoTracking()
            .Where(x => x.Level == level && x.Role == UserRole.Student)
            .OrderBy(x => x.Username)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<User>> GetTeachersAsync(CancellationToken cancellationToken = default)
        => await _db.Users.AsNoTracking()
            .Include(x => x.TeacherLevels)
            .Where(x => x.Role == UserRole.Teacher)
            .OrderBy(x => x.Username)
            .ToListAsync(cancellationToken);

    public Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default)
        => _db.Users.Include(x => x.Devices).Include(x => x.TeacherLevels)
            .FirstOrDefaultAsync(x => x.Username == username, cancellationToken);

    public Task<User?> FindByIdAsync(long id, CancellationToken cancellationToken = default)
        => _db.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<User?> FindByIdWithTeacherLevelsAsync(long id, CancellationToken cancellationToken = default)
        => _db.Users.Include(x => x.TeacherLevels)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<User> CreateAsync(
        string username,
        string passwordHash,
        UserRole role,
        LanguageLevel? level = null,
        IReadOnlyList<LanguageLevel>? teacherLevels = null,
        CancellationToken cancellationToken = default)
    {
        if (role == UserRole.Student && level is null)
            throw new InvalidOperationException("دانشجو باید یک سطح زبانی داشته باشد.");

        if (role == UserRole.Teacher && (teacherLevels is null || teacherLevels.Count == 0))
            throw new InvalidOperationException("مدرس باید حداقل یک سطح زبانی مجاز داشته باشد.");

        var user = new User
        {
            Username = username.Trim(),
            PasswordHash = passwordHash,
            Role = role,
            Level = role == UserRole.Student ? level : null
        };

        if (role == UserRole.Teacher)
        {
            foreach (var lv in teacherLevels!.Distinct())
                user.TeacherLevels.Add(new TeacherLevel { Level = lv });
        }

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
        var user = await _db.Users.Include(x => x.TeacherLevels).FirstAsync(x => x.Id == userId, cancellationToken);
        user.Role = role;
        if (role != UserRole.Student)
            user.Level = null;
        if (role != UserRole.Teacher)
            user.TeacherLevels.Clear();
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SetLevelAsync(long userId, LanguageLevel? level, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstAsync(x => x.Id == userId, cancellationToken);
        if (user.Role != UserRole.Student)
            throw new InvalidOperationException("فقط دانشجو می‌تواند یک سطح زبانی تکی داشته باشد.");
        user.Level = level;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SetTeacherLevelsAsync(long userId, IReadOnlyList<LanguageLevel> levels, CancellationToken cancellationToken = default)
    {
        if (levels is null || levels.Count == 0)
            throw new InvalidOperationException("حداقل یک سطح زبانی برای مدرس لازم است.");

        var user = await _db.Users.Include(x => x.TeacherLevels).FirstAsync(x => x.Id == userId, cancellationToken);
        if (user.Role != UserRole.Teacher)
            throw new InvalidOperationException("فقط برای نقش مدرس می‌توان سطوح مجاز تعریف کرد.");

        user.TeacherLevels.Clear();
        foreach (var lv in levels.Distinct())
            user.TeacherLevels.Add(new TeacherLevel { UserId = userId, Level = lv });

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LanguageLevel>> GetTeacherLevelsAsync(long userId, CancellationToken cancellationToken = default)
    {
        return await _db.TeacherLevels.AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.Level)
            .ToListAsync(cancellationToken);
    }

    public async Task SetActiveAsync(long userId, bool isActive, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstAsync(x => x.Id == userId, cancellationToken);
        user.IsActive = isActive;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
