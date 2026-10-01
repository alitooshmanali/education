using Lesson.Domain.Common;

namespace Lesson.Domain.Entities;

public sealed class QuizAttempt : BaseEntity
{
    public long QuizId { get; set; }
    public Quiz? Quiz { get; set; }

    public long UserId { get; set; }
    public User? User { get; set; }

    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }

    public decimal ScoreEarned { get; set; }
    public bool IsCompleted { get; set; }

    /// <summary>Seconds elapsed when completed or abandoned.</summary>
    public int TimeSpentSeconds { get; set; }

    public ICollection<QuizAnswer> Answers { get; set; } = new List<QuizAnswer>();
}
