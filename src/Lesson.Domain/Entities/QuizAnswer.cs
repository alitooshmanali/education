using Lesson.Domain.Common;

namespace Lesson.Domain.Entities;

public sealed class QuizAnswer : BaseEntity
{
    public long AttemptId { get; set; }
    public QuizAttempt? Attempt { get; set; }

    public long QuestionId { get; set; }
    public QuizQuestion? Question { get; set; }

    public long? SelectedOptionId { get; set; }
    public QuizOption? SelectedOption { get; set; }

    public bool IsCorrect { get; set; }
    public decimal ScoreEarned { get; set; }
    public DateTimeOffset AnsweredAt { get; set; } = DateTimeOffset.UtcNow;
}
