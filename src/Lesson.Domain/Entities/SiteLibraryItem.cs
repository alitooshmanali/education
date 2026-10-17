using Lesson.Domain.Common;
using Lesson.Domain.Enums;

namespace Lesson.Domain.Entities;

/// <summary>
/// Public marketing-site library item managed by Admin (Podcast / Story / Music / Book).
/// Voice-only podcasts play inline; items with text open a detail view.
/// </summary>
public sealed class SiteLibraryItem : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? BodyText { get; set; }

    public SiteLibraryCategory Category { get; set; }

    /// <summary>Relative path under wwwroot or media store for cover image.</summary>
    public string? ImagePath { get; set; }

    public long? ImageContentId { get; set; }
    public LessonContent? ImageContent { get; set; }

    public long? AudioContentId { get; set; }
    public LessonContent? AudioContent { get; set; }

    /// <summary>When true and audio exists, click plays audio without opening detail.</summary>
    public bool IsVoiceOnly { get; set; }

    public int SortOrder { get; set; }
}
