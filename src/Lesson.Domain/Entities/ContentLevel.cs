using Lesson.Domain.Enums;

namespace Lesson.Domain.Entities;

/// <summary>
/// Many-to-many: a content item can appear in multiple language levels.
/// </summary>
public sealed class ContentLevel
{
    public long ContentId { get; set; }
    public LessonContent? Content { get; set; }

    public LanguageLevel Level { get; set; }
}
