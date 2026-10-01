using Lesson.Domain.Enums;

namespace Lesson.Infrastructure.Abstractions;

public sealed record FeedbackDto(
    long Id,
    long UserId,
    string Username,
    FeedbackTargetType TargetType,
    long TargetId,
    int Rating,
    string? Comment,
    DateTimeOffset CreatedAt);

public sealed record SatisfactionSummaryDto(
    double AverageRating,
    int TotalCount,
    int SatisfiedCount,
    int NeutralCount,
    int DissatisfiedCount,
    double SatisfactionPercent);

public interface IFeedbackService
{
    Task SubmitAsync(long userId, FeedbackTargetType targetType, long targetId, int rating, string? comment, CancellationToken cancellationToken = default);
    Task<FeedbackDto?> GetUserFeedbackAsync(long userId, FeedbackTargetType targetType, long targetId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FeedbackDto>> GetAllAsync(FeedbackTargetType? targetType = null, CancellationToken cancellationToken = default);
    Task<SatisfactionSummaryDto> GetSatisfactionSummaryAsync(CancellationToken cancellationToken = default);
}
