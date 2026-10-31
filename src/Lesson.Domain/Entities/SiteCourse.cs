using Lesson.Domain.Common;
using Lesson.Domain.Enums;

namespace Lesson.Domain.Entities;

/// <summary>دورهٔ عمومی سایت (کارت روی صفحه زبان انگلیسی).</summary>
public sealed class SiteCourse : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    public CourseOfferLevel Level { get; set; }

    /// <summary>شهریه به تومان (عدد صحیح).</summary>
    public decimal TuitionAmount { get; set; }

    public int SessionCount { get; set; }

    /// <summary>مدت هر جلسه به دقیقه (اختیاری).</summary>
    public int? SessionDurationMinutes { get; set; }

    public long? ImageContentId { get; set; }
    public LessonContent? ImageContent { get; set; }

    public long? VideoContentId { get; set; }
    public LessonContent? VideoContent { get; set; }

    public int SortOrder { get; set; }
}
