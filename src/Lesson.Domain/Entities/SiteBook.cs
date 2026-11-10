using Lesson.Domain.Common;

namespace Lesson.Domain.Entities;

/// <summary>کتاب فروشگاهی/نمایشی سایت — فقط عنوان، تصویر، قیمت.</summary>
public sealed class SiteBook : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public long? ImageContentId { get; set; }
    public LessonContent? ImageContent { get; set; }
    public decimal Price { get; set; }
    public int SortOrder { get; set; }
}
