using Lesson.Domain.Entities;

namespace Lesson.Infrastructure.Abstractions;

public sealed record DeviceValidationResult(bool Allowed, string? Reason, UserDevice? Device);

public interface IDeviceService
{
    Task<List<UserDevice>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<DeviceValidationResult> ValidateOrRegisterAsync(long userId, string deviceIdentifier, CancellationToken cancellationToken = default);
    Task<bool> IsSessionValidAsync(long userId, string deviceIdentifier, string sessionKey, CancellationToken cancellationToken = default);
    Task RevokeDeviceAsync(long deviceId, CancellationToken cancellationToken = default);
}
