using Lesson.Domain.Enums;

namespace Lesson.Domain;

/// <summary>
/// هر مهارت ۰ تا ۱۰؛ جمع ۰ تا ۴۰.
/// Average = Total/4 → نگاشت به CEFR.
/// اگر ضعیف‌ترین مهارت &lt; ۳ و میانگین در B2+ باشد، یک پله پایین می‌آید.
/// </summary>
public static class PlacementScoreCalculator
{
    public const decimal MaxPerSkill = 10m;
    public const decimal MaxTotal = 40m;

    public static (decimal Total, decimal Average, LanguageLevel Level) Compute(
        decimal listening,
        decimal reading,
        decimal writing,
        decimal vocabulary)
    {
        listening = Clamp(listening);
        reading = Clamp(reading);
        writing = Clamp(writing);
        vocabulary = Clamp(vocabulary);

        var total = listening + reading + writing + vocabulary;
        var average = total / 4m;
        var level = MapAverageToLevel(average);

        var min = Math.Min(Math.Min(listening, reading), Math.Min(writing, vocabulary));
        if (min < 3m && level >= LanguageLevel.B2)
            level = (LanguageLevel)((int)level - 1);

        return (Round1(total), Round1(average), level);
    }

    public static LanguageLevel MapAverageToLevel(decimal average)
    {
        // Average 0–10
        if (average < 2.5m) return LanguageLevel.A1;
        if (average < 4.5m) return LanguageLevel.A2;
        if (average < 6.1m) return LanguageLevel.B1;
        if (average < 7.6m) return LanguageLevel.B2;
        if (average < 8.8m) return LanguageLevel.C1;
        return LanguageLevel.C2;
    }

    public static string LevelLabel(LanguageLevel level) => level switch
    {
        LanguageLevel.A1 => "A1 — مبتدی",
        LanguageLevel.A2 => "A2 — پایه",
        LanguageLevel.B1 => "B1 — متوسط",
        LanguageLevel.B2 => "B2 — متوسط رو به بالا",
        LanguageLevel.C1 => "C1 — پیشرفته",
        LanguageLevel.C2 => "C2 — مسلط",
        _ => level.ToString()
    };

    private static decimal Clamp(decimal v) =>
        v < 0 ? 0 : v > MaxPerSkill ? MaxPerSkill : v;

    private static decimal Round1(decimal v) =>
        Math.Round(v, 1, MidpointRounding.AwayFromZero);
}
