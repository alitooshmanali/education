using Lesson.Domain.Common;
using Lesson.Domain.Enums;

namespace Lesson.Domain.Entities;

/// <summary>دوره عمومی سایت (زبان / کامپیوتر / نقاشی / موسیقی).</summary>
public sealed class SiteCourse : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    public SubjectArea Subject { get; set; } = SubjectArea.English;

    public CourseOfferLevel Level { get; set; }

    public decimal TuitionAmount { get; set; }
    public int SessionCount { get; set; }
    public int? SessionDurationMinutes { get; set; }

    /// <summary>UTC — نمایش جلالی در UI</summary>
    public DateTimeOffset? StartsAt { get; set; }

    public long? ImageContentId { get; set; }
    public LessonContent? ImageContent { get; set; }

    public long? VideoContentId { get; set; }
    public LessonContent? VideoContent { get; set; }

    public int SortOrder { get; set; }
}
