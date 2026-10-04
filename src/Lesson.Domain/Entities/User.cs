using Lesson.Domain.Common;
using Lesson.Domain.Enums;

namespace Lesson.Domain.Entities;

public sealed class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }

    /// <summary>Exactly one level for students. Null for Admin/Teacher (teachers use TeacherLevels).</summary>
    public LanguageLevel? Level { get; set; }

    /// <summary>سطوح مجاز مدرس برای بارگذاری محتوا. فقط برای Role=Teacher معنا دارد.</summary>
    public ICollection<TeacherLevel> TeacherLevels { get; set; } = new List<TeacherLevel>();

    public ICollection<Permission> Permissions { get; set; } = new List<Permission>();
    public ICollection<UserDevice> Devices { get; set; } = new List<UserDevice>();
    public ICollection<AuthenticationSession> AuthenticationSessions { get; set; } = new List<AuthenticationSession>();
    public ICollection<ContentProgress> ContentProgresses { get; set; } = new List<ContentProgress>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public ICollection<QuizAssignment> QuizAssignments { get; set; } = new List<QuizAssignment>();
    public ICollection<QuizAttempt> QuizAttempts { get; set; } = new List<QuizAttempt>();
    public ICollection<ContentFeedback> Feedbacks { get; set; } = new List<ContentFeedback>();
    public ICollection<QuizRetakeGrant> QuizRetakeGrants { get; set; } = new List<QuizRetakeGrant>();
}
