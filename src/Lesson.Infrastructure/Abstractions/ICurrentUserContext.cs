using Lesson.Domain.Enums;

namespace Lesson.Infrastructure.Abstractions;

public interface ICurrentUserContext
{
    long? UserId { get; }
    UserRole? Role { get; }
    string? DeviceIdentifier { get; }
    string? SessionKey { get; }
}
