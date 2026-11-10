using Lesson.Domain.Enums;

namespace Lesson.Infrastructure.Abstractions;

public sealed record PlacementGameItemDto(
    long Id,
    PlacementGameKind Kind,
    string? Level,
    string? QuestionText,
    string? OptionsJson,
    int? AnswerIndex,
    string? Word,
    string? Line1,
    string? Line2,
    string? Hint,
    long? AudioContentId,
    string? AudioUrl,
    string? SpeakText,
    string? StoryPrompt,
    int SortOrder,
    bool IsActive);

public interface IPlacementGameBankService
{
    Task<IReadOnlyList<PlacementGameItemDto>> GetAllAdminAsync(PlacementGameKind? kind = null, CancellationToken ct = default);
    Task<PlacementGameItemDto> UpsertAsync(
        long? id,
        PlacementGameKind kind,
        string? level,
        string? questionText,
        string? optionsJson,
        int? answerIndex,
        string? word,
        string? line1,
        string? line2,
        string? hint,
        long? audioContentId,
        string? speakText,
        string? storyPrompt,
        int sortOrder,
        bool isActive,
        CancellationToken ct = default);
    Task HardDeleteAsync(long id, CancellationToken ct = default);
    object GetPublicBank();
}
