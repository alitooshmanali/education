using System.Globalization;

namespace Lesson.Domain;

/// <summary>
/// تبدیل تاریخ جلالی ↔ میلادی و نمایش فارسی با TimeZone ایران.
/// ذخیره در DB همیشه UTC / میلادی؛ نمایش همیشه جلالی.
/// </summary>
public static class PersianDate
{
    public static readonly TimeZoneInfo IranTimeZone = GetIranTimeZone();

    private static TimeZoneInfo GetIranTimeZone()
    {
        foreach (var id in new[] { "Iran Standard Time", "Asia/Tehran" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch { /* try next */ }
        }
        return TimeZoneInfo.CreateCustomTimeZone("Iran", TimeSpan.FromHours(3.5), "Iran", "Iran");
    }

    public static DateTimeOffset ToIran(DateTimeOffset utcOrAny)
        => TimeZoneInfo.ConvertTime(utcOrAny, IranTimeZone);

    public static string Format(DateTimeOffset? value, bool includeTime = true)
    {
        if (value is null) return "—";
        var local = ToIran(value.Value);
        var (jy, jm, jd) = ToJalali(local.Year, local.Month, local.Day);
        var date = $"{jy:0000}/{jm:00}/{jd:00}";
        if (!includeTime) return ToPersianDigits(date);
        var time = $"{local.Hour:00}:{local.Minute:00}";
        return ToPersianDigits($"{date} {time}");
    }

    public static string FormatDayName(DateTimeOffset? value)
    {
        if (value is null) return "—";
        var local = ToIran(value.Value);
        var name = local.DayOfWeek switch
        {
            DayOfWeek.Saturday => "شنبه",
            DayOfWeek.Sunday => "یکشنبه",
            DayOfWeek.Monday => "دوشنبه",
            DayOfWeek.Tuesday => "سه‌شنبه",
            DayOfWeek.Wednesday => "چهارشنبه",
            DayOfWeek.Thursday => "پنجشنبه",
            DayOfWeek.Friday => "جمعه",
            _ => ""
        };
        return name;
    }

    public static string FormatFull(DateTimeOffset? value)
    {
        if (value is null) return "—";
        return $"{FormatDayName(value)} {Format(value, includeTime: true)}";
    }

    /// <summary>
    /// ورودی جلالی: yyyy/MM/dd یا yyyy/MM/dd HH:mm → UTC DateTimeOffset
    /// </summary>
    public static DateTimeOffset? ParseJalaliToUtc(string? jalaliDate, string? timeHm = null)
    {
        if (string.IsNullOrWhiteSpace(jalaliDate)) return null;
        var s = FromPersianDigits(jalaliDate.Trim()).Replace('-', '/');
        var parts = s.Split(new[] { ' ', 'T' }, StringSplitOptions.RemoveEmptyEntries);
        var dparts = parts[0].Split('/');
        if (dparts.Length != 3) return null;
        if (!int.TryParse(dparts[0], out var jy)) return null;
        if (!int.TryParse(dparts[1], out var jm)) return null;
        if (!int.TryParse(dparts[2], out var jd)) return null;

        var hour = 0;
        var minute = 0;
        var timePart = timeHm;
        if (string.IsNullOrWhiteSpace(timePart) && parts.Length > 1) timePart = parts[1];
        if (!string.IsNullOrWhiteSpace(timePart))
        {
            var t = FromPersianDigits(timePart.Trim()).Split(':');
            if (t.Length >= 1) int.TryParse(t[0], out hour);
            if (t.Length >= 2) int.TryParse(t[1], out minute);
        }

        var (gy, gm, gd) = ToGregorian(jy, jm, jd);
        var unspecified = new DateTime(gy, gm, gd, hour, minute, 0, DateTimeKind.Unspecified);
        var offset = IranTimeZone.GetUtcOffset(unspecified);
        var dto = new DateTimeOffset(unspecified, offset);
        return dto.ToUniversalTime();
    }

    public static string ToPersianDigits(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        var chars = input.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (chars[i] >= '0' && chars[i] <= '9')
                chars[i] = (char)('۰' + (chars[i] - '0'));
        }
        return new string(chars);
    }

    public static string FromPersianDigits(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        var chars = input.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (chars[i] >= '۰' && chars[i] <= '۹')
                chars[i] = (char)('0' + (chars[i] - '۰'));
            else if (chars[i] >= '٠' && chars[i] <= '٩')
                chars[i] = (char)('0' + (chars[i] - '٠'));
        }
        return new string(chars);
    }

    // ——— Jalali algorithms (Astro / civil) ———
    public static (int Year, int Month, int Day) ToJalali(int gy, int gm, int gd)
    {
        int[] g_d_m = { 0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334 };
        int gy2 = (gm > 2) ? (gy + 1) : gy;
        int days = 355666 + (365 * gy) + ((gy2 + 3) / 4) - ((gy2 + 99) / 100) + ((gy2 + 399) / 400) + gd + g_d_m[gm - 1];
        int jy = -1595 + (33 * (days / 12053));
        days %= 12053;
        jy += 4 * (days / 1461);
        days %= 1461;
        if (days > 365)
        {
            jy += (days - 1) / 365;
            days = (days - 1) % 365;
        }
        int jm, jd;
        if (days < 186)
        {
            jm = 1 + (days / 31);
            jd = 1 + (days % 31);
        }
        else
        {
            jm = 7 + ((days - 186) / 30);
            jd = 1 + ((days - 186) % 30);
        }
        return (jy, jm, jd);
    }

    public static (int Year, int Month, int Day) ToGregorian(int jy, int jm, int jd)
    {
        jy += 1595;
        int days = -355668 + (365 * jy) + ((jy / 33) * 8) + (((jy % 33) + 3) / 4) + jd
                   + ((jm < 7) ? (jm - 1) * 31 : ((jm - 7) * 30 + 186));
        int gy = 400 * (days / 146097);
        days %= 146097;
        if (days > 36524)
        {
            gy += 100 * (--days / 36524);
            days %= 36524;
            if (days >= 365) days++;
        }
        gy += 4 * (days / 1461);
        days %= 1461;
        if (days > 365)
        {
            gy += (days - 1) / 365;
            days = (days - 1) % 365;
        }
        int gd = days + 1;
        int[] sal_a = { 0, 31, ((gy % 4 == 0 && gy % 100 != 0) || (gy % 400 == 0)) ? 29 : 28,
            31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
        int gm = 0;
        for (gm = 1; gm <= 12 && gd > sal_a[gm]; gm++) gd -= sal_a[gm];
        return (gy, gm, gd);
    }
}
