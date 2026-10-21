using Lesson.Domain.Common;
using Lesson.Domain.Enums;

namespace Lesson.Domain.Entities;

/// <summary>
/// Public marketing-site library item (Podcast / Story / Music / Book / Video).
/// Videos support previous/next, multi related and multi suggested lists.
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

    public bool IsVoiceOnly { get; set; }

    public long? PreviousItemId { get; set; }
    public long? NextItemId { get; set; }

    /// <summary>Comma-separated related item ids (multi).</summary>
    public string? RelatedItemIds { get; set; }

    /// <summary>Comma-separated suggested item ids (multi).</summary>
    public string? SuggestedItemIds { get; set; }

    public int SortOrder { get; set; }
}
