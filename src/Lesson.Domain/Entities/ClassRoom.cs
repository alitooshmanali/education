using Lesson.Domain.Common;
using Lesson.Domain.Enums;

namespace Lesson.Domain.Entities;

/// <summary>
/// کلاس آموزشی: یک کد کلاس (مثلاً ۱۰۱) در یک سطح و حوزه، با یک مدرس و برنامهٔ زمانی.
/// محتوا (ویدیو، داستان، پادکست، آزمون) توسط مدرس برای همین کلاس/سطح بارگذاری می‌شود.
/// </summary>
public sealed class ClassRoom : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public SubjectArea Subject { get; set; }
    public LanguageLevel Level { get; set; }

    public long? TeacherId { get; set; }
    public User? Teacher { get; set; }

    public int Capacity { get; set; } = 12;
    public int EnrolledCount { get; set; }

    public ClassStatus Status { get; set; } = ClassStatus.NoTeacher;

    /// <summary>شروع و پایان یکسان برای همهٔ روزهای این کلاس.</summary>
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }

    public ICollection<ClassScheduleDay> ScheduleDays { get; set; } = new List<ClassScheduleDay>();
    public ICollection<LessonContent> Contents { get; set; } = new List<LessonContent>();
    public ICollection<Story> Stories { get; set; } = new List<Story>();
    public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
}
