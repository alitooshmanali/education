using Lesson.Domain.Common;
using Lesson.Domain.Enums;

namespace Lesson.Domain.Entities;

/// <summary>
/// Public marketing-site library item (Podcast / Story / Music / Book / Video).
/// Videos support previous/next chain and related suggestions (YouTube-style learning path).
/// </summary>
public sealed class SiteLibraryItem : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? BodyText { get; set; }

    public SiteLibraryCategory Category { get; set; }

    public string? ImagePath { get; set; }

    public long? ImageContentId { get; set; }
    public LessonContent? ImageContent { get; set; }

    public long? AudioContentId { get; set; }
    public LessonContent? AudioContent { get; set; }

    public long? VideoContentId { get; set; }
    public LessonContent? VideoContent { get; set; }

    /// <summary>When true and audio exists, click plays audio without opening detail.</summary>
    public bool IsVoiceOnly { get; set; }

    /// <summary>Previous video in the learning path (must watch before).</summary>
    public long? PreviousItemId { get; set; }

    /// <summary>Next video in the learning path.</summary>
    public long? NextItemId { get; set; }

    /// <summary>Comma-separated related/suggested item ids.</summary>
    public string? RelatedItemIds { get; set; }

    public int SortOrder { get; set; }
}
