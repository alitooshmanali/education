using Lesson.Domain.Entities;
using Lesson.Domain.Enums;

namespace Lesson.Infrastructure.Abstractions;

public sealed record ContentDto(
    long Id,
    string OriginalFileName,
    string DisplayName,
    string FileExtension,
    string MimeType,
    long FileSize,
    ContentType Type,
    double? DurationSeconds,
    long FolderId,
    string FolderPath,
    IReadOnlyList<LanguageLevel> Levels);

public sealed record UploadContentRequest(
    long FolderId,
    string OriginalFileName,
    string DisplayName,
    Stream Content,
    long FileSize,
    IReadOnlyList<LanguageLevel> Levels,
    ContentType? PreferredType = null);

public sealed record ContentProgressDto(
    long? ContentId,
    long? StoryId,
    double LastPositionSeconds,
    double MaxWatchedSeconds,
    double ProgressPercent,
    bool IsCompleted);

public sealed record UserProgressSummaryDto(
    int TotalVideos,
    int CompletedVideos,
    double VideoProgressPercent,
    int TotalStories,
    int CompletedStories,
    double StoryProgressPercent,
    int TotalQuizzes,
    int CompletedQuizzes,
    double QuizProgressPercent,
    double OverallProgressPercent);

public interface IContentService
{
    Task<IReadOnlyList<ContentDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ContentDto>> GetForUserAsync(long userId, bool isAdmin, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ContentDto>> GetByLevelAsync(LanguageLevel level, CancellationToken cancellationToken = default);
    Task<LessonContent?> FindAsync(long contentId, CancellationToken cancellationToken = default);
    Task<LessonContent> UploadAsync(UploadContentRequest request, CancellationToken cancellationToken = default);
    Task MoveAsync(long contentId, long folderId, CancellationToken cancellationToken = default);
    Task SetLevelsAsync(long contentId, IReadOnlyList<LanguageLevel> levels, CancellationToken cancellationToken = default);
    Task DeleteAsync(long contentId, CancellationToken cancellationToken = default);

    Task<ContentProgressDto?> GetProgressAsync(long userId, long contentId, CancellationToken cancellationToken = default);
    Task UpdateProgressAsync(long userId, long contentId, double positionSeconds, double? durationSeconds, CancellationToken cancellationToken = default);
    Task<UserProgressSummaryDto> GetUserProgressSummaryAsync(long userId, CancellationToken cancellationToken = default);
}
