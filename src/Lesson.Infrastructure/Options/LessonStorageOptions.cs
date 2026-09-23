namespace Lesson.Infrastructure.Options;

/// <summary>
/// تنظیمات ذخیره‌سازی. محتوا دیگر روی دیسک ذخیره نمی‌شود و داخل جدول Contents (بايت) است.
/// ساختار پوشه‌ها فقط منطقی و در جدول Folders است (مثل درخت ویندوز، بدون ساخت فیزیکی).
/// </summary>
public sealed class LessonStorageOptions
{
    /// <summary>حداکثر حجم مجاز آپلود (پیش‌فرض ۲۰ مگابایت).</summary>
    public long MaximumFileSizeInBytes { get; set; } = 20L * 1024 * 1024;

    /// <summary>
    /// دیگر استفاده نمی‌شود. هر پسوندی مجاز است.
    /// فقط برای سازگاری با کدهای قدیمی نگه داشته شده.
    /// </summary>
    public string[] AllowedExtensions { get; set; } = Array.Empty<string>();
}
