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

    public async Task<DeviceValidationResult> ValidateOrRegisterAsync(long userId, string deviceIdentifier, CancellationToken cancellationToken = default)
    {
        var device = await _db.UserDevices.FirstOrDefaultAsync(x => x.UserId == userId && x.DeviceIdentifier == deviceIdentifier, cancellationToken);
        if (device is not null)
        {
            if (device.RevokedAt is not null)
            {
                return new DeviceValidationResult(false, "Device revoked.", device);
            }

            device.LastSeenAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return new DeviceValidationResult(true, null, device);
        }

        var hasActive = await _db.UserDevices.AnyAsync(x => x.UserId == userId && x.RevokedAt == null, cancellationToken);
        if (hasActive)
        {
            return new DeviceValidationResult(false, "Another device is already active.", null);
        }

        device = new UserDevice
        {
            UserId = userId,
            DeviceIdentifier = deviceIdentifier,
            FirstSeenAt = DateTimeOffset.UtcNow,
            LastSeenAt = DateTimeOffset.UtcNow
        };
        _db.UserDevices.Add(device);
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
        var device = await _db.UserDevices.Include(x => x.AuthenticationSessions).Include(x => x.VideoWatchSessions).FirstAsync(x => x.Id == deviceId, cancellationToken);
        device.RevokedAt = DateTimeOffset.UtcNow;
        foreach (var session in device.AuthenticationSessions)
        {
            session.RevokedAt = DateTimeOffset.UtcNow;
        }
        foreach (var watch in device.VideoWatchSessions)
        {
            watch.EndedAt = DateTimeOffset.UtcNow;
        }
        await _db.SaveChangesAsync(cancellationToken);
    }
}
