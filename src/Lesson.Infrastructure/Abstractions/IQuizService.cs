using Lesson.Domain.Entities;
using Lesson.Domain.Enums;

namespace Lesson.Infrastructure.Abstractions;

public sealed record QuizListItemDto(
    long Id,
    string Title,
    QuizType Type,
    LanguageLevel Level,
    bool IsLevelWide,
    int TimeLimitMinutes,
    decimal TotalScore,
    int QuestionCount,
    bool HasAttempted,
    bool CanRetake,
    decimal? LastScore);

public sealed record QuizDetailDto(
    long Id,
    string Title,
    string IntroText,
    long? IntroImageContentId,
    QuizType Type,
    LanguageLevel Level,
    bool IsLevelWide,
    int TimeLimitMinutes,
    decimal TotalScore,
    IReadOnlyList<QuizQuestionDto> Questions);

public sealed record QuizQuestionDto(
    long Id,
    int SortOrder,
    string Text,
    long? ImageContentId,
    decimal Score,
    IReadOnlyList<QuizOptionDto> Options);

public sealed record QuizOptionDto(
    long Id,
    int SortOrder,
    string Text,
    bool IsCorrect);

public sealed record CreateQuizRequest(
    string Title,
    string IntroText,
    long? IntroImageContentId,
    QuizType Type,
    LanguageLevel Level,
    bool IsLevelWide,
    int TimeLimitMinutes,
    decimal TotalScore,
    long? FolderId,
    IReadOnlyList<CreateQuestionRequest> Questions,
    IReadOnlyList<long>? AssignedUserIds);

public sealed record CreateQuestionRequest(
    string Text,
    long? ImageContentId,
    decimal Score,
    int SortOrder,
    IReadOnlyList<CreateOptionRequest> Options);

public sealed record CreateOptionRequest(
    string Text,
    bool IsCorrect,
    int SortOrder);

public sealed record QuizAttemptStateDto(
    long AttemptId,
    long QuizId,
    DateTimeOffset StartedAt,
    int TimeLimitMinutes,
    int RemainingSeconds,
    decimal ScoreEarned,
    bool IsCompleted,
    IReadOnlyList<long> AnsweredQuestionIds,
    IReadOnlyList<QuizAnswerResultDto> Answers);

public sealed record QuizAnswerResultDto(
    long QuestionId,
    long? SelectedOptionId,
    bool IsCorrect,
    decimal ScoreEarned);

public sealed record SubmitAnswerResult(
    bool IsCorrect,
    decimal ScoreEarned,
    long CorrectOptionId);

public sealed record QuizReportRowDto(
    long UserId,
    string Username,
    LanguageLevel? Level,
    long QuizId,
    string QuizTitle,
    decimal ScoreEarned,
    decimal TotalScore,
    bool IsCompleted,
    DateTimeOffset? CompletedAt,
    int TimeSpentSeconds);

public interface IQuizService
{
    Task<IReadOnlyList<QuizListItemDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<QuizListItemDto>> GetForStudentAsync(long userId, CancellationToken cancellationToken = default);
    Task<QuizDetailDto?> GetDetailAsync(long quizId, bool includeCorrectAnswers, CancellationToken cancellationToken = default);
    Task<Quiz> CreateAsync(CreateQuizRequest request, CancellationToken cancellationToken = default);
    Task UpdateAsync(long quizId, CreateQuizRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(long quizId, CancellationToken cancellationToken = default);

    Task<bool> CanStartAsync(long userId, long quizId, CancellationToken cancellationToken = default);
    Task<QuizAttemptStateDto> StartAttemptAsync(long userId, long quizId, CancellationToken cancellationToken = default);
    Task<QuizAttemptStateDto?> GetActiveAttemptAsync(long userId, long quizId, CancellationToken cancellationToken = default);
    Task<SubmitAnswerResult> SubmitAnswerAsync(long userId, long attemptId, long questionId, long optionId, CancellationToken cancellationToken = default);
    Task<QuizAttemptStateDto> CompleteAttemptAsync(long userId, long attemptId, CancellationToken cancellationToken = default);

    Task GrantRetakeAsync(long quizId, long userId, long adminUserId, CancellationToken cancellationToken = default);
    Task AssignUsersAsync(long quizId, IReadOnlyList<long> userIds, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<QuizReportRowDto>> GetReportsAsync(LanguageLevel? level, long? quizId, CancellationToken cancellationToken = default);
}
