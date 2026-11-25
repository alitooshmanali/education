using System.Security.Cryptography;
using Lesson.Domain.Security;
using Lesson.Domain.Entities;
using Lesson.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Lesson.Infrastructure.Abstractions;

namespace Lesson.Infrastructure.Services;

public sealed class AuthenticationService : IAuthenticationService
{
    private readonly LessonDbContext _db;
    private readonly IDeviceService _devices;

    public AuthenticationService(LessonDbContext db, IDeviceService devices)
    {
        _db = db;
        _devices = devices;
    }

    public async Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Username == request.Username && x.IsActive, cancellationToken);
        if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            return new LoginResult(false, "Invalid credentials.", null, null);
        }

        // Keep device mutation and session creation in one unit of work to avoid
        // an extra database round-trip on every successful login.
        var device = await _devices.ValidateOrRegisterAsync(
            user.Id, request.DeviceIdentifier, cancellationToken, saveChanges: false);
        if (!device.Allowed || device.Device is null)
        {
            return new LoginResult(false, device.Reason, null, null);
        }

        var sessionKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        _db.AuthenticationSessions.Add(new AuthenticationSession
        {
            UserId = user.Id,
            UserDevice = device.Device,
            SessionKey = sessionKey
        });
        await _db.SaveChangesAsync(cancellationToken);
        return new LoginResult(true, null, user, sessionKey);
    }

    public async Task LogoutAsync(long userId, string sessionKey, CancellationToken cancellationToken = default)
    {
        var session = await _db.AuthenticationSessions.FirstOrDefaultAsync(x => x.UserId == userId && x.SessionKey == sessionKey, cancellationToken);
        if (session is not null)
        {
            session.RevokedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
