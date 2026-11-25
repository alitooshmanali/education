using Lesson.Domain.Enums;

namespace Lesson.Infrastructure.Abstractions;

public interface ICurrentUserContext
{
    Task EnsureInitializedAsync();
    long? UserId { get; }
    UserRole? Role { get; }
    string? DeviceIdentifier { get; }
    string? SessionKey { get; }
}
