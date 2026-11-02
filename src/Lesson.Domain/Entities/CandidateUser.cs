using Lesson.Domain.Common;
using Lesson.Domain.Enums;

namespace Lesson.Domain.Entities;

/// <summary>
/// مهمان سایت که آزمون تعیین سطح داده (هنوز User ثبت‌نام‌شده نیست).
/// </summary>
public sealed class CandidateUser : BaseEntity
{
    /// <summary>شماره موبایل نرمال‌شده (مثلاً 09xxxxxxxxx)</summary>
    public string PhoneNumber { get; set; } = string.Empty;

    public decimal? ListeningScore { get; set; }
    public decimal? ReadingScore { get; set; }
    public decimal? WritingScore { get; set; }
    public decimal? VocabularyScore { get; set; }

    public decimal? TotalScore { get; set; }

    /// <summary>سطح پیشنهادی محاسبه‌شده (A1..C2)</summary>
    public LanguageLevel? SuggestionLevel { get; set; }

    public CandidateStatus Status { get; set; } = CandidateStatus.Started;

    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>یادداشت ادمین / پیگیری تماس</summary>
    public string? Notes { get; set; }
}
