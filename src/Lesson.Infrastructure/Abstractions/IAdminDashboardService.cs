using Lesson.Domain.Entities;
using Lesson.Infrastructure.Abstractions;

namespace Lesson.Infrastructure.Abstractions;

public sealed record AdminRecentUploadDto(
    long Id,
    string Title,
    string FileName,
    string Type,
    long FileSize,
    bool IsActive,
    DateTimeOffset CreatedAt);

public sealed record AdminDashboardDataDto(
    int UsersTotal,
    int UsersActive,
    int UsersInactive,
    int Students,
    int TeachersTotal,
    int TeachersActive,
    int Admins,
    int ClassesTotal,
    int ClassesActive,
    int ClassesNoTeacher,
    int ClassesFull,
    int ActiveContents,
    int Folders,
    int Quizzes,
    int WebsiteSlots,
    int WebsiteLibraryItems,
    int WebsiteBooks,
    int WebsiteCourses,
    int Candidates,
    int QuizAttempts,
    int AuditEvents,
    SatisfactionSummaryDto Satisfaction,
    IReadOnlyList<FeedbackDto> RecentFeedback,
    IReadOnlyList<ClassRoomDto> UnassignedClasses,
    IReadOnlyList<ClassRoomDto> FullClasses,
    IReadOnlyList<AdminRecentUploadDto> RecentUploads);

public interface IAdminDashboardService
{
    Task<AdminDashboardDataDto> GetAsync(CancellationToken cancellationToken = default);
}
