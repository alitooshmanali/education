using Lesson.Domain.Entities;
using Lesson.Domain.Enums;
using Lesson.Infrastructure.Abstractions;
using Lesson.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lesson.Infrastructure.Services;

public sealed class FeedbackService : IFeedbackService
{
    private readonly LessonDbContext _db;
    public FeedbackService(LessonDbContext db) => _db = db;

    public async Task SubmitAsync(long userId, FeedbackTargetType targetType, long targetId, int rating, string? comment, CancellationToken cancellationToken = default)
    {
        if (rating is < 1 or > 5)
            throw new InvalidOperationException("امتیاز باید بین ۱ تا ۵ باشد.");

        var existing = await _db.ContentFeedbacks
            .FirstOrDefaultAsync(f => f.UserId == userId && f.TargetType == targetType && f.TargetId == targetId, cancellationToken);

        if (existing is not null)
        {
            existing.Rating = rating;
            existing.Comment = comment?.Trim();
        }
        else
        {
            _db.ContentFeedbacks.Add(new ContentFeedback
            {
                UserId = userId,
                TargetType = targetType,
                TargetId = targetId,
                Rating = rating,
                Comment = comment?.Trim()
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<FeedbackDto?> GetUserFeedbackAsync(long userId, FeedbackTargetType targetType, long targetId, CancellationToken cancellationToken = default)
    {
        var f = await _db.ContentFeedbacks.AsNoTracking()
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.UserId == userId && x.TargetType == targetType && x.TargetId == targetId, cancellationToken);
        return f is null ? null : ToDto(f);
    }

    public async Task<IReadOnlyList<FeedbackDto>> GetAllAsync(FeedbackTargetType? targetType = null, CancellationToken cancellationToken = default)
    {
        var q = _db.ContentFeedbacks.AsNoTracking().Include(x => x.User).AsQueryable();
        if (targetType.HasValue)
            q = q.Where(x => x.TargetType == targetType.Value);

        var list = await q.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
        return list.Select(ToDto).ToList();
    }

    public async Task<SatisfactionSummaryDto> GetSatisfactionSummaryAsync(CancellationToken cancellationToken = default)
    {
        var ratings = await _db.ContentFeedbacks.AsNoTracking().Select(f => f.Rating).ToListAsync(cancellationToken);
        if (ratings.Count == 0)
            return new SatisfactionSummaryDto(0, 0, 0, 0, 0, 0);

        var avg = ratings.Average();
        var satisfied = ratings.Count(r => r >= 4);
        var neutral = ratings.Count(r => r == 3);
        var dissatisfied = ratings.Count(r => r <= 2);
        var pct = Math.Round(satisfied * 100.0 / ratings.Count, 1);

        return new SatisfactionSummaryDto(
            Math.Round(avg, 2), ratings.Count, satisfied, neutral, dissatisfied, pct);
    }

    private static FeedbackDto ToDto(ContentFeedback f) => new(
        f.Id, f.UserId, f.User?.Username ?? "", f.TargetType, f.TargetId,
        f.Rating, f.Comment, f.CreatedAt);
}
