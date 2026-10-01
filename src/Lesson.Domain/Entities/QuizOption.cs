using Lesson.Domain.Common;

namespace Lesson.Domain.Entities;

public sealed class QuizOption : BaseEntity
{
    public long QuestionId { get; set; }
    public QuizQuestion? Question { get; set; }

    public int SortOrder { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}
