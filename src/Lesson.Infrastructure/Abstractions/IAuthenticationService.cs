using Lesson.Domain.Entities;

namespace Lesson.Infrastructure.Abstractions;

public sealed record LoginRequest(string Username, string Password, string DeviceIdentifier);
public sealed record LoginResult(bool Success, string? FailureReason, User? User, string? SessionKey);

public interface IAuthenticationService
{
    Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task LogoutAsync(long userId, string sessionKey, CancellationToken cancellationToken = default);
}
