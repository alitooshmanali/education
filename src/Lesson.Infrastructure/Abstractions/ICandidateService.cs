using Lesson.Domain.Enums;

namespace Lesson.Infrastructure.Abstractions;

public sealed record PlacementQuestionDto(
    string Id,
    string Skill,          // Listening | Reading | Writing | Vocabulary | Speaking
    string Text,
    IReadOnlyList<string> Options,
    string? AudioUrl = null,
    string? ListenText = null,   // متنی که باید شنیده شود (TTS/صوت)
    string? SpeakText = null);   // عبارتی که کاربر باید تلفظ کند

public sealed record PlacementStartResult(long CandidateId, string PhoneNumber);

public sealed record PlacementAnswerDto(string QuestionId, int SelectedIndex);

public sealed record PlacementSubmitRequest(long CandidateId, IReadOnlyList<PlacementAnswerDto> Answers);

public sealed record PlacementResultDto(
    long CandidateId,
    string PhoneNumber,
    decimal ListeningScore,
    decimal ReadingScore,
    decimal WritingScore,
    decimal VocabularyScore,
    decimal TotalScore,
    decimal AverageScore,
    string SuggestionLevel,
    string SuggestionLabel);

public sealed record CandidateUserAdminDto(
    long Id,
    string PhoneNumber,
    decimal? ListeningScore,
    decimal? ReadingScore,
    decimal? WritingScore,
    decimal? VocabularyScore,
    decimal? TotalScore,
    string? SuggestionLevel,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    string? Notes);

public interface ICandidateService
{
    Task<PlacementStartResult> StartAsync(string phoneNumber, CancellationToken ct = default);
    IReadOnlyList<PlacementQuestionDto> GetQuestions();
    Task<PlacementResultDto> SubmitAsync(long candidateId, IReadOnlyList<PlacementAnswerDto> answers, CancellationToken ct = default);
    Task<IReadOnlyList<CandidateUserAdminDto>> GetAllAdminAsync(CancellationToken ct = default);
    Task<PlacementResultDto> SubmitGameAsync(
        long candidateId,
        decimal listeningScore,
        decimal readingScore,
        decimal writingScore,
        decimal vocabularyScore,
        string? suggestionLevel,
        CancellationToken ct = default);
    object GetGameBank();
}
