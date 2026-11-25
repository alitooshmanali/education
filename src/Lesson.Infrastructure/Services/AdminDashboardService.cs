using Lesson.Domain.Enums;
using Lesson.Infrastructure.Abstractions;
using Lesson.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lesson.Infrastructure.Services;

/// <summary>
/// Returns only dashboard aggregates and a small recent activity window.
/// Avoids loading every user, class, quiz, folder and feedback row into memory.
/// </summary>
public sealed class AdminDashboardService(LessonDbContext db) : IAdminDashboardService
{
    public async Task<AdminDashboardDataDto> GetAsync(CancellationToken cancellationToken = default)
    {
        // A single SQL projection with scalar subqueries replaces several full-list queries.
        var totals = await db.Users.AsNoTracking()
            .Select(_ => new
            {
                UsersTotal = db.Users.Count(),
                UsersActive = db.Users.Count(x => x.IsActive),
                Students = db.Users.Count(x => x.Role == UserRole.Student),
                Teachers = db.Users.Count(x => x.Role == UserRole.Teacher),
                TeachersActive = db.Users.Count(x => x.Role == UserRole.Teacher && x.IsActive),
                Admins = db.Users.Count(x => x.Role == UserRole.Admin),
                ClassesTotal = db.ClassRooms.Count(),
                ClassesActive = db.ClassRooms.Count(x => x.Status == ClassStatus.Active),
                ClassesNoTeacher = db.ClassRooms.Count(x => x.Status == ClassStatus.NoTeacher || x.TeacherId == null),
                ClassesFull = db.ClassRooms.Count(x => x.Status == ClassStatus.Full),
                ActiveContents = db.Contents.Count(x => x.IsActive),
                Folders = db.Folders.Count(),
                Quizzes = db.Quizzes.Count(),
                WebsiteSlots = db.SitePageSlots.Count(),
                WebsiteLibraryItems = db.SiteLibraryItems.Count(),
                WebsiteBooks = db.SiteBooks.Count(),
                WebsiteCourses = db.SiteCourses.Count(),
                Candidates = db.CandidateUsers.Count(),
                QuizAttempts = db.QuizAttempts.Count(),
                AuditEvents = db.AuditLogs.Count()
            })
            .FirstOrDefaultAsync(cancellationToken);

        var feedback = await db.ContentFeedbacks.AsNoTracking()
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Average = g.Average(x => (double)x.Rating),
                Satisfied = g.Count(x => x.Rating >= 4),
                Neutral = g.Count(x => x.Rating == 3),
                Dissatisfied = g.Count(x => x.Rating <= 2)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var satisfaction = feedback is null
            ? new SatisfactionSummaryDto(0, 0, 0, 0, 0, 0)
            : new SatisfactionSummaryDto(
                Math.Round(feedback.Average, 2),
                feedback.Total,
                feedback.Satisfied,
                feedback.Neutral,
                feedback.Dissatisfied,
                Math.Round(feedback.Satisfied * 100.0 / Math.Max(1, feedback.Total), 1));

        var recentFeedback = await db.ContentFeedbacks.AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Take(8)
            .Select(x => new FeedbackDto(
                x.Id,
                x.UserId,
                x.User != null ? x.User.Username : string.Empty,
                x.TargetType,
                x.TargetId,
                x.Rating,
                x.Comment,
                x.CreatedAt))
            .ToListAsync(cancellationToken);

        var unassignedRows = await db.ClassRooms.AsNoTracking()
            .Where(x => x.TeacherId == null && x.Status != ClassStatus.Inactive)
            .OrderByDescending(x => x.Id)
            .Take(5)
            .Select(x => new
            {
                x.Id, x.Code, x.Title, x.Subject, x.Level, x.TeacherId,
                x.Capacity, x.EnrolledCount, x.Status, x.StartTime, x.EndTime
            })
            .ToListAsync(cancellationToken);
        var unassigned = unassignedRows.Select(x => new ClassRoomDto(
            x.Id, x.Code, x.Title, x.Subject, x.Level, x.TeacherId, null,
            x.Capacity, x.EnrolledCount, x.Status, x.StartTime, x.EndTime,
            Array.Empty<ClassScheduleDayDto>())).ToList();

        var fullRows = await db.ClassRooms.AsNoTracking()
            .Where(x => x.Status == ClassStatus.Full)
            .OrderByDescending(x => x.Id)
            .Take(3)
            .Select(x => new
            {
                x.Id, x.Code, x.Title, x.Subject, x.Level, x.TeacherId,
                x.Capacity, x.EnrolledCount, x.Status, x.StartTime, x.EndTime
            })
            .ToListAsync(cancellationToken);
        var full = fullRows.Select(x => new ClassRoomDto(
            x.Id, x.Code, x.Title, x.Subject, x.Level, x.TeacherId, null,
            x.Capacity, x.EnrolledCount, x.Status, x.StartTime, x.EndTime,
            Array.Empty<ClassScheduleDayDto>())).ToList();

        // Project only metadata: never select LessonContent.Content (varbinary(max)) here.
        var recentUploads = await db.Contents.AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Take(8)
            .Select(x => new AdminRecentUploadDto(
                x.Id,
                x.DisplayName,
                x.OriginalFileName,
                x.Type.ToString(),
                x.FileSize,
                x.IsActive,
                x.CreatedAt))
            .ToListAsync(cancellationToken);

        var usersTotal = totals?.UsersTotal ?? 0;
        var usersActive = totals?.UsersActive ?? 0;
        return new AdminDashboardDataDto(
            usersTotal,
            usersActive,
            usersTotal - usersActive,
            totals?.Students ?? 0,
            totals?.Teachers ?? 0,
            totals?.TeachersActive ?? 0,
            totals?.Admins ?? 0,
            totals?.ClassesTotal ?? 0,
            totals?.ClassesActive ?? 0,
            totals?.ClassesNoTeacher ?? 0,
            totals?.ClassesFull ?? 0,
            totals?.ActiveContents ?? 0,
            totals?.Folders ?? 0,
            totals?.Quizzes ?? 0,
            totals?.WebsiteSlots ?? 0,
            totals?.WebsiteLibraryItems ?? 0,
            totals?.WebsiteBooks ?? 0,
            totals?.WebsiteCourses ?? 0,
            totals?.Candidates ?? 0,
            totals?.QuizAttempts ?? 0,
            totals?.AuditEvents ?? 0,
            satisfaction,
            recentFeedback,
            unassigned,
            full,
            recentUploads);
    }
}
