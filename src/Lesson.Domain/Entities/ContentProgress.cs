using Lesson.Domain.Common;

namespace Lesson.Domain.Entities;

/// <summary>
/// Tracks how far a student has watched/read a piece of content.
/// For videos: LastPositionSeconds is the resume point.
/// If IsCompleted, next play starts from the beginning.
/// </summary>
public sealed class ContentProgress : BaseEntity
{
    public long UserId { get; set; }
    public User? User { get; set; }

    public long ContentId { get; set; }
    public LessonContent? Content { get; set; }

    /// <summary>Last known playback position in seconds (for video/audio).</summary>
    public double LastPositionSeconds { get; set; }

    /// <summary>Maximum continuous or cumulative watched seconds (used for progress %).</summary>
    public double MaxWatchedSeconds { get; set; }

    /// <summary>0..100 progress percentage.</summary>
    public double ProgressPercent { get; set; }

    public bool IsCompleted { get; set; }

    public DateTimeOffset LastWatchedAt { get; set; } = DateTimeOffset.UtcNow;
}
