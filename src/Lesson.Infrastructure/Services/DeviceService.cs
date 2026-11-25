using Lesson.Domain.Entities;
using Lesson.Infrastructure.Abstractions;
using Lesson.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lesson.Infrastructure.Services;

public sealed class DeviceService : IDeviceService
{
    private readonly LessonDbContext _db;
    public DeviceService(LessonDbContext db) => _db = db;

    public Task<List<UserDevice>> GetAllAsync(CancellationToken cancellationToken = default)
        => _db.UserDevices.Include(x => x.User).OrderByDescending(x => x.LastSeenAt).ToListAsync(cancellationToken);

    public async Task<DeviceValidationResult> ValidateOrRegisterAsync(long userId, string deviceIdentifier, CancellationToken cancellationToken = default, bool saveChanges = true)
    {
        var device = await _db.UserDevices.FirstOrDefaultAsync(x => x.UserId == userId && x.DeviceIdentifier == deviceIdentifier, cancellationToken);
        if (device is not null)
        {
            if (device.RevokedAt is not null)
            {
                return new DeviceValidationResult(false, "Device revoked.", device);
            }

            device.LastSeenAt = DateTimeOffset.UtcNow;
            if (saveChanges)
                await _db.SaveChangesAsync(cancellationToken);
            return new DeviceValidationResult(true, null, device);
        }

        // Revoke other active devices so login is not blocked (single active device policy)
        var otherActives = await _db.UserDevices
            .Where(x => x.UserId == userId && x.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var other in otherActives)
        {
            other.RevokedAt = DateTimeOffset.UtcNow;
        }

        device = new UserDevice
        {
            UserId = userId,
            DeviceIdentifier = deviceIdentifier,
            FirstSeenAt = DateTimeOffset.UtcNow,
            LastSeenAt = DateTimeOffset.UtcNow
        };
        _db.UserDevices.Add(device);
        if (saveChanges)
            await _db.SaveChangesAsync(cancellationToken);
        return new DeviceValidationResult(true, null, device);
    }

    public async Task<bool> IsSessionValidAsync(long userId, string deviceIdentifier, string sessionKey, CancellationToken cancellationToken = default)
    {
        return await _db.AuthenticationSessions.AnyAsync(x =>
            x.UserId == userId &&
            x.SessionKey == sessionKey &&
            x.RevokedAt == null &&
            x.UserDevice != null &&
            x.UserDevice.DeviceIdentifier == deviceIdentifier &&
            x.UserDevice.RevokedAt == null, cancellationToken);
    }

    public async Task RevokeDeviceAsync(long deviceId, CancellationToken cancellationToken = default)
    {
        var device = await _db.UserDevices
            .Include(x => x.AuthenticationSessions)
            .FirstAsync(x => x.Id == deviceId, cancellationToken);

        device.RevokedAt = DateTimeOffset.UtcNow;
        foreach (var session in device.AuthenticationSessions)
        {
            session.RevokedAt = DateTimeOffset.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
