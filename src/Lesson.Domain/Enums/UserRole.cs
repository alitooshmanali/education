namespace Lesson.Domain.Enums;

public enum UserRole
{
    Admin = 1,
    Student = 2,
    /// <summary>مدرس — فقط محتوای سطوح تخصیص‌یافته را می‌تواند بارگذاری/ویرایش کند.</summary>
    Teacher = 3
}
