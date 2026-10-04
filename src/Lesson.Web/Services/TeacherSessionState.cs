using Lesson.Domain.Enums;
using Lesson.Infrastructure.Abstractions;

namespace Lesson.Web.Services;

/// <summary>
/// وضعیت انتخاب سطح/کلاس در پنل مدرس — صفحات فقط با تغییر این state دادهٔ همان بخش را لود می‌کنند.
/// </summary>
public sealed class TeacherSessionState
{
    public LanguageLevel? SelectedLevel { get; private set; }
    public long? SelectedClassId { get; private set; }
    public ClassRoomDto? SelectedClass { get; private set; }

    public event Action? Changed;

    public void SetLevel(LanguageLevel? level)
    {
        if (SelectedLevel == level) return;
        SelectedLevel = level;
        SelectedClassId = null;
        SelectedClass = null;
        Changed?.Invoke();
    }

    public void SetClass(ClassRoomDto? cls)
    {
        SelectedClassId = cls?.Id;
        SelectedClass = cls;
        if (cls is not null)
            SelectedLevel = cls.Level;
        Changed?.Invoke();
    }
}
