using Lesson.Domain.Common;
using Lesson.Domain.Enums;

namespace Lesson.Domain.Entities;

public sealed class Quiz : BaseEntity
{
    public string Title { get; set; } = string.Empty;

    /// <summary>Intro text shown before the quiz starts (with optional image).</summary>
    public string IntroText { get; set; } = string.Empty;

    public long? IntroImageContentId { get; set; }
    public LessonContent? IntroImageContent { get; set; }

    public QuizType Type { get; set; } = QuizType.General;
    public LanguageLevel Level { get; set; }

    /// <summary>
    /// For Grammar quizzes: if true, all students in the level can take it.
    /// If false, only assigned users (QuizAssignments).
    /// General quizzes are always level-wide.
    /// </summary>
    public bool IsLevelWide { get; set; } = true;

    public int TimeLimitMinutes { get; set; } = 10;
    public decimal TotalScore { get; set; }

    public long? FolderId { get; set; }
    public Folder? Folder { get; set; }

    public long? ClassId { get; set; }
    public ClassRoom? Class { get; set; }
    public long? CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }

    public ICollection<QuizQuestion> Questions { get; set; } = new List<QuizQuestion>();
    public ICollection<QuizAssignment> Assignments { get; set; } = new List<QuizAssignment>();
    public ICollection<QuizAttempt> Attempts { get; set; } = new List<QuizAttempt>();
    public ICollection<QuizRetakeGrant> RetakeGrants { get; set; } = new List<QuizRetakeGrant>();
    public ICollection<ContentFeedback> Feedbacks { get; set; } = new List<ContentFeedback>();
}
