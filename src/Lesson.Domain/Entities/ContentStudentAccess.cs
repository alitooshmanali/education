namespace Lesson.Domain.Entities;

/// <summary>
/// دسترسی دانش‌آموز به یک محتوای خاص (ویدیو/پادکست) وقتی IsRestricted=true.
/// </summary>
public sealed class ContentStudentAccess
{
    public long ContentId { get; set; }
    public LessonContent? Content { get; set; }

    public long UserId { get; set; }
    public User? User { get; set; }
}
