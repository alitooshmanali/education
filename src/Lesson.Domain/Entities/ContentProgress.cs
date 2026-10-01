using Lesson.Domain.Common;

namespace Lesson.Domain.Entities;

/// <summary>
/// Tracks student progress on a video (LessonContent) or a story.
/// Exactly one of ContentId or StoryId is set.
/// </summary>
public sealed class ContentProgress : BaseEntity
{
    public long UserId { get; set; }
    public User? User { get; set; }

    public long? ContentId { get; set; }
    public LessonContent? Content { get; set; }

    public long? StoryId { get; set; }
    public Story? Story { get; set; }

    /// <summary>Last known playback position in seconds (for video/audio).</summary>
    public double LastPositionSeconds { get; set; }

    /// <summary>Maximum continuous or cumulative watched seconds.</summary>
    public double MaxWatchedSeconds { get; set; }

    /// <summary>0..100 progress percentage.</summary>
    public double ProgressPercent { get; set; }

    public bool IsCompleted { get; set; }

    public DateTimeOffset LastWatchedAt { get; set; } = DateTimeOffset.UtcNow;
}
