using Lesson.Domain.Entities;
using Lesson.Domain.Enums;

namespace Lesson.Infrastructure.Abstractions;

public sealed record TeacherLevelOption(LanguageLevel Level, int ClassCount);

public sealed record TeacherClassStudentDto(
    long UserId,
    string Username,
    LanguageLevel? Level,
    bool IsActive);

public sealed record TeacherDashboardDto(
    long ClassId,
    string ClassCode,
    string ClassTitle,
    LanguageLevel Level,
    int StudentCount,
    int VideoCount,
    int PodcastCount,
    int StoryCount,
    int QuizCount,
    int FolderCount);

public sealed record TeacherContentListItemDto(
    long Id,
    string DisplayName,
    ContentType Type,
    long FolderId,
    string FolderName,
    bool IsRestricted,
    long FileSize);

public sealed record TeacherQuizListItemDto(
    long Id,
    string Title,
    QuizType Type,
    int TimeLimitMinutes,
    decimal TotalScore,
    int QuestionCount,
    long? FolderId);

public sealed record TeacherStoryListItemDto(
    long Id,
    string Title,
    long FolderId,
    string FolderName,
    bool HasImage,
    bool HasVoice);

public sealed record TeacherStudentReportDto(
    long UserId,
    string Username,
    int VideosCompleted,
    int StoriesCompleted,
    int QuizzesTaken,
    decimal? AvgQuizScore,
    double? AvgSatisfaction);

public interface ITeacherWorkspaceService
{
    Task<IReadOnlyList<TeacherLevelOption>> GetMyLevelsAsync(long teacherId, CancellationToken ct = default);
    Task<IReadOnlyList<ClassRoomDto>> GetMyClassesAsync(long teacherId, LanguageLevel? level = null, CancellationToken ct = default);
    Task EnsureOwnsClassAsync(long teacherId, long classId, CancellationToken ct = default);

    Task<TeacherDashboardDto?> GetDashboardAsync(long teacherId, long classId, CancellationToken ct = default);
    Task<IReadOnlyList<TeacherClassStudentDto>> GetClassStudentsAsync(long teacherId, long classId, CancellationToken ct = default);

    Task<IReadOnlyList<FolderNodeDto>> GetClassFoldersAsync(long teacherId, long classId, CancellationToken ct = default);
    Task<Folder> CreateClassFolderAsync(long teacherId, long classId, string name, long? parentFolderId, CancellationToken ct = default);
    Task DeleteClassFolderAsync(long teacherId, long folderId, CancellationToken ct = default);

    Task<IReadOnlyList<TeacherContentListItemDto>> GetClassContentsAsync(long teacherId, long classId, ContentType type, CancellationToken ct = default);
    Task SetContentRestrictionAsync(long teacherId, long contentId, bool isRestricted, IReadOnlyList<long>? allowedUserIds, CancellationToken ct = default);

    Task<IReadOnlyList<TeacherQuizListItemDto>> GetClassQuizzesAsync(long teacherId, long classId, CancellationToken ct = default);
    Task<IReadOnlyList<TeacherStoryListItemDto>> GetClassStoriesAsync(long teacherId, long classId, CancellationToken ct = default);

    Task<IReadOnlyList<TeacherStudentReportDto>> GetClassStudentReportsAsync(long teacherId, long classId, CancellationToken ct = default);
}
