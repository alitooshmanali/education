using Lesson.Domain.Common;

namespace Lesson.Domain.Entities;

public sealed class QuizQuestion : BaseEntity
{
    public long QuizId { get; set; }
    public Quiz? Quiz { get; set; }

    public int SortOrder { get; set; }
    public string Text { get; set; } = string.Empty;

    public long? ImageContentId { get; set; }
    public LessonContent? ImageContent { get; set; }

    /// <summary>Score awarded when answered correctly.</summary>
    public decimal Score { get; set; }

    public ICollection<QuizOption> Options { get; set; } = new List<QuizOption>();
    public ICollection<QuizAnswer> Answers { get; set; } = new List<QuizAnswer>();
}
