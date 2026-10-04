namespace Lesson.Domain.Entities;

/// <summary>
/// روزهای هفتهٔ یک کلاس (چند روز با همان ساعت شروع/پایان کلاس).
/// DayOfWeek: 0=Sunday ... 6=Saturday (استاندارد .NET). برای ایران معمولاً شنبه=6.
/// </summary>
public sealed class ClassScheduleDay
{
    public long ClassId { get; set; }
    public ClassRoom? Class { get; set; }

    /// <summary>System.DayOfWeek as int (0=Sunday .. 6=Saturday).</summary>
    public int DayOfWeek { get; set; }
}
