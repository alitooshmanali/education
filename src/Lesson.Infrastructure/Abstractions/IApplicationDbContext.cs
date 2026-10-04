using Lesson.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lesson.Infrastructure.Abstractions;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Folder> Folders { get; }
    DbSet<LessonContent> Contents { get; }
    DbSet<ContentLevel> ContentLevels { get; }
    DbSet<ContentProgress> ContentProgresses { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<UserDevice> UserDevices { get; }
    DbSet<AuthenticationSession> AuthenticationSessions { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<Story> Stories { get; }
    DbSet<Quiz> Quizzes { get; }
    DbSet<QuizQuestion> QuizQuestions { get; }
    DbSet<QuizOption> QuizOptions { get; }
    DbSet<QuizAssignment> QuizAssignments { get; }
    DbSet<QuizAttempt> QuizAttempts { get; }
    DbSet<QuizAnswer> QuizAnswers { get; }
    DbSet<QuizRetakeGrant> QuizRetakeGrants { get; }
    DbSet<ContentFeedback> ContentFeedbacks { get; }
    DbSet<TeacherLevel> TeacherLevels { get; }
    DbSet<ClassRoom> ClassRooms { get; }
    DbSet<ClassScheduleDay> ClassScheduleDays { get; }
    DbSet<ContentStudentAccess> ContentStudentAccesses { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
