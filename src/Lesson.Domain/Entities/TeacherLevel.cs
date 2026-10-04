using Lesson.Domain.Enums;

namespace Lesson.Domain.Entities;

/// <summary>
/// سطوح زبانی که یک مدرس مجاز به مدیریت محتوا برای آن‌هاست.
/// مدرس فقط می‌تواند محتوا را برای همین سطوح بارگذاری یا ویرایش کند.
/// </summary>
public sealed class TeacherLevel
{
    public long UserId { get; set; }
    public User? User { get; set; }

    public LanguageLevel Level { get; set; }
}
