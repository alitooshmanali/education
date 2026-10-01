using Lesson.Domain.Common;
using Lesson.Domain.Enums;

namespace Lesson.Domain.Entities;

/// <summary>
/// Student rating (1-5) and optional comment on Video, Story, or Quiz.
/// </summary>
public sealed class ContentFeedback : BaseEntity
{
    public long UserId { get; set; }
    public User? User { get; set; }

    public FeedbackTargetType TargetType { get; set; }
    public long TargetId { get; set; }

    /// <summary>1 (very dissatisfied) .. 5 (very satisfied).</summary>
    public int Rating { get; set; }

    public string? Comment { get; set; }
}
