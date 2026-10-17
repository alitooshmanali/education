using Lesson.Domain.Common;
using Lesson.Domain.Enums;

namespace Lesson.Domain.Entities;

/// <summary>
/// Configurable media slot on the public marketing site (carousel / circle / hero).
/// Images are stored as LessonContent and streamed via /api/site/media/{contentId}.
/// </summary>
public sealed class SitePageSlot : BaseEntity
{
    /// <summary>e.g. home, english, english-podcast, painting</summary>
    public string PageKey { get; set; } = "home";

    public SiteSlotType SlotType { get; set; }

    public string? Title { get; set; }
    public string? Subtitle { get; set; }
    /// <summary>Client hash route e.g. #/english</summary>
    public string? LinkHref { get; set; }

    public long? ImageContentId { get; set; }
    public LessonContent? ImageContent { get; set; }

    public int SortOrder { get; set; }
}
