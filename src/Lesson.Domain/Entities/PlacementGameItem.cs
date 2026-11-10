using Lesson.Domain.Common;
using Lesson.Domain.Enums;

namespace Lesson.Domain.Entities;

/// <summary>آیتم بانک بازی تعیین سطح (گرامر / واژگان / شنیداری / مکالمه).</summary>
public sealed class PlacementGameItem : BaseEntity
{
    public PlacementGameKind Kind { get; set; }

    /// <summary>A1..C2 یا IELTS</summary>
    public string? Level { get; set; }

    public string? QuestionText { get; set; }

    /// <summary>JSON array of option strings for Grammar</summary>
    public string? OptionsJson { get; set; }

    public int? AnswerIndex { get; set; }

    /// <summary>Vocab target word</summary>
    public string? Word { get; set; }

    public string? Line1 { get; set; }
    public string? Line2 { get; set; }
    public string? Hint { get; set; }

    public long? AudioContentId { get; set; }
    public LessonContent? AudioContent { get; set; }

    public string? SpeakText { get; set; }
    public string? StoryPrompt { get; set; }

    public int SortOrder { get; set; }
}
