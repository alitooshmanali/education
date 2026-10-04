using Lesson.Domain.Entities;
using Lesson.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Lesson.Infrastructure.Persistence;

public sealed class LessonDbContext : DbContext, Abstractions.IApplicationDbContext
{
    public LessonDbContext(DbContextOptions<LessonDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Folder> Folders => Set<Folder>();
    public DbSet<LessonContent> Contents => Set<LessonContent>();
    public DbSet<ContentLevel> ContentLevels => Set<ContentLevel>();
    public DbSet<ContentProgress> ContentProgresses => Set<ContentProgress>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserDevice> UserDevices => Set<UserDevice>();
    public DbSet<AuthenticationSession> AuthenticationSessions => Set<AuthenticationSession>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Story> Stories => Set<Story>();
    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<QuizQuestion> QuizQuestions => Set<QuizQuestion>();
    public DbSet<QuizOption> QuizOptions => Set<QuizOption>();
    public DbSet<QuizAssignment> QuizAssignments => Set<QuizAssignment>();
    public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();
    public DbSet<QuizAnswer> QuizAnswers => Set<QuizAnswer>();
    public DbSet<QuizRetakeGrant> QuizRetakeGrants => Set<QuizRetakeGrant>();
    public DbSet<ContentFeedback> ContentFeedbacks => Set<ContentFeedback>();
    public DbSet<TeacherLevel> TeacherLevels => Set<TeacherLevel>();
    public DbSet<ClassRoom> ClassRooms => Set<ClassRoom>();
    public DbSet<ClassScheduleDay> ClassScheduleDays => Set<ClassScheduleDay>();
    public DbSet<ContentStudentAccess> ContentStudentAccesses => Set<ContentStudentAccess>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LessonDbContext).Assembly);

        // ---- User ----
        modelBuilder.Entity<User>(b =>
        {
            b.Property(x => x.Level).HasConversion<int?>();
        });

        // ---- TeacherLevel (مدرس ↔ سطوح مجاز) ----
        modelBuilder.Entity<TeacherLevel>(b =>
        {
            b.ToTable("TeacherLevels");
            b.HasKey(x => new { x.UserId, x.Level });
            b.Property(x => x.Level).HasConversion<int>();
            b.HasOne(x => x.User)
                .WithMany(u => u.TeacherLevels)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(x => x.Level);
        });

        
        // ---- ClassRoom ----
        modelBuilder.Entity<ClassRoom>(b =>
        {
            b.ToTable("ClassRooms");
            b.HasKey(x => x.Id);
            b.Property(x => x.Code).HasMaxLength(64).IsRequired();
            b.Property(x => x.Title).HasMaxLength(300).IsRequired();
            b.Property(x => x.Subject).HasConversion<int>();
            b.Property(x => x.Level).HasConversion<int>();
            b.Property(x => x.Status).HasConversion<int>();
            b.HasIndex(x => x.Code);
            b.HasIndex(x => x.TeacherId);
            b.HasOne(x => x.Teacher)
                .WithMany()
                .HasForeignKey(x => x.TeacherId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ClassScheduleDay>(b =>
        {
            b.ToTable("ClassScheduleDays");
            b.HasKey(x => new { x.ClassId, x.DayOfWeek });

        modelBuilder.Entity<ContentStudentAccess>(b =>
        {
            b.ToTable("ContentStudentAccesses");
            b.HasKey(x => new { x.ContentId, x.UserId });
            b.HasOne(x => x.Content).WithMany(c => c.StudentAccesses).HasForeignKey(x => x.ContentId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

            b.HasOne(x => x.Class)
                .WithMany(c => c.ScheduleDays)
                .HasForeignKey(x => x.ClassId)
                .OnDelete(DeleteBehavior.Cascade);
        });

// ---- LessonContent ----
        modelBuilder.Entity<LessonContent>(b =>
        {
            b.ToTable("Contents");
            b.HasKey(x => x.Id);
            b.Property(x => x.OriginalFileName).HasMaxLength(512).IsRequired();
            b.Property(x => x.DisplayName).HasMaxLength(512).IsRequired();
            b.Property(x => x.FileExtension).HasMaxLength(32);
            b.Property(x => x.MimeType).HasMaxLength(128);
            b.Property(x => x.Content).HasColumnType("varbinary(max)");
            b.HasOne(x => x.Folder)
                .WithMany(f => f.Contents)
                .HasForeignKey(x => x.FolderId)
                .OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => x.FolderId);
            b.HasIndex(x => x.Type);
        });

        // ---- ContentLevel (M2M) ----
        modelBuilder.Entity<ContentLevel>(b =>
        {
            b.ToTable("ContentLevels");
            b.HasKey(x => new { x.ContentId, x.Level });
            b.Property(x => x.Level).HasConversion<int>();
            b.HasOne(x => x.Content)
                .WithMany(c => c.Levels)
                .HasForeignKey(x => x.ContentId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(x => x.Level);
        });

        // ---- ContentProgress ----
        modelBuilder.Entity<ContentProgress>(b =>
        {
            b.ToTable("ContentProgresses");
            b.HasKey(x => x.Id);
            b.HasOne(x => x.User)
                .WithMany(u => u.ContentProgresses)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Content)
                .WithMany(c => c.ProgressRecords)
                .HasForeignKey(x => x.ContentId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);
            b.HasOne(x => x.Story)
                .WithMany(s => s.ProgressRecords)
                .HasForeignKey(x => x.StoryId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);
            b.HasIndex(x => new { x.UserId, x.ContentId });
            b.HasIndex(x => new { x.UserId, x.StoryId });
        });

        // ---- Story ----
        modelBuilder.Entity<Story>(b =>
        {
            b.ToTable("Stories");
            b.HasKey(x => x.Id);
            b.Property(x => x.Title).HasMaxLength(300).IsRequired();
            b.Property(x => x.Text).HasColumnType("nvarchar(max)").IsRequired();
            b.Property(x => x.Level).HasConversion<int>().IsRequired();
            b.HasOne(x => x.Folder)
                .WithMany(f => f.Stories)
                .HasForeignKey(x => x.FolderId)
                .OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.ImageContent)
                .WithMany()
                .HasForeignKey(x => x.ImageContentId)
                .OnDelete(DeleteBehavior.SetNull);
            b.HasOne(x => x.VoiceContent)
                .WithMany()
                .HasForeignKey(x => x.VoiceContentId)
                .OnDelete(DeleteBehavior.SetNull);
            b.HasIndex(x => x.Level);
            b.HasIndex(x => x.FolderId);
        });

        // ---- Quiz ----
        modelBuilder.Entity<Quiz>(b =>
        {
            b.ToTable("Quizzes");
            b.HasKey(x => x.Id);
            b.Property(x => x.Title).HasMaxLength(300).IsRequired();
            b.Property(x => x.IntroText).HasColumnType("nvarchar(max)");
            b.Property(x => x.Type).HasConversion<int>();
            b.Property(x => x.Level).HasConversion<int>();
            b.Property(x => x.TotalScore).HasPrecision(10, 2);
            b.HasOne(x => x.Folder)
                .WithMany(f => f.Quizzes)
                .HasForeignKey(x => x.FolderId)
                .OnDelete(DeleteBehavior.SetNull);
            b.HasOne(x => x.IntroImageContent)
                .WithMany()
                .HasForeignKey(x => x.IntroImageContentId)
                .OnDelete(DeleteBehavior.SetNull);
            b.HasIndex(x => x.Level);
            b.HasIndex(x => x.Type);
        });

        modelBuilder.Entity<QuizQuestion>(b =>
        {
            b.ToTable("QuizQuestions");
            b.HasKey(x => x.Id);
            b.Property(x => x.Text).HasColumnType("nvarchar(max)").IsRequired();
            b.Property(x => x.Score).HasPrecision(10, 2);
            b.HasOne(x => x.Quiz)
                .WithMany(q => q.Questions)
                .HasForeignKey(x => x.QuizId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.ImageContent)
                .WithMany()
                .HasForeignKey(x => x.ImageContentId)
                .OnDelete(DeleteBehavior.SetNull);
            b.HasIndex(x => new { x.QuizId, x.SortOrder });
        });

        modelBuilder.Entity<QuizOption>(b =>
        {
            b.ToTable("QuizOptions");
            b.HasKey(x => x.Id);
            b.Property(x => x.Text).HasMaxLength(1000).IsRequired();
            b.HasOne(x => x.Question)
                .WithMany(q => q.Options)
                .HasForeignKey(x => x.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(x => new { x.QuestionId, x.SortOrder });
        });

        modelBuilder.Entity<QuizAssignment>(b =>
        {
            b.ToTable("QuizAssignments");
            b.HasKey(x => new { x.QuizId, x.UserId });
            b.HasOne(x => x.Quiz)
                .WithMany(q => q.Assignments)
                .HasForeignKey(x => x.QuizId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.User)
                .WithMany(u => u.QuizAssignments)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<QuizAttempt>(b =>
        {
            b.ToTable("QuizAttempts");
            b.HasKey(x => x.Id);
            b.Property(x => x.ScoreEarned).HasPrecision(10, 2);
            b.HasOne(x => x.Quiz)
                .WithMany(q => q.Attempts)
                .HasForeignKey(x => x.QuizId)
                .OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.User)
                .WithMany(u => u.QuizAttempts)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(x => new { x.QuizId, x.UserId });
        });

        modelBuilder.Entity<QuizAnswer>(b =>
        {
            b.ToTable("QuizAnswers");
            b.HasKey(x => x.Id);
            b.Property(x => x.ScoreEarned).HasPrecision(10, 2);
            b.HasOne(x => x.Attempt)
                .WithMany(a => a.Answers)
                .HasForeignKey(x => x.AttemptId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Question)
                .WithMany(q => q.Answers)
                .HasForeignKey(x => x.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.SelectedOption)
                .WithMany()
                .HasForeignKey(x => x.SelectedOptionId)
                .OnDelete(DeleteBehavior.SetNull);
            b.HasIndex(x => new { x.AttemptId, x.QuestionId }).IsUnique();
        });

        modelBuilder.Entity<QuizRetakeGrant>(b =>
        {
            b.ToTable("QuizRetakeGrants");
            b.HasKey(x => x.Id);
            b.HasOne(x => x.Quiz)
                .WithMany(q => q.RetakeGrants)
                .HasForeignKey(x => x.QuizId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.User)
                .WithMany(u => u.QuizRetakeGrants)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.GrantedByUser)
                .WithMany()
                .HasForeignKey(x => x.GrantedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => new { x.QuizId, x.UserId, x.IsUsed });
        });

        // ---- ContentFeedback ----
        modelBuilder.Entity<ContentFeedback>(b =>
        {
            b.ToTable("ContentFeedbacks");
            b.HasKey(x => x.Id);
            b.Property(x => x.TargetType).HasConversion<int>();
            b.Property(x => x.Comment).HasMaxLength(2000);
            b.HasOne(x => x.User)
                .WithMany(u => u.Feedbacks)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(x => new { x.TargetType, x.TargetId });
            b.HasIndex(x => new { x.UserId, x.TargetType, x.TargetId }).IsUnique();
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var entry in ChangeTracker.Entries<Lesson.Domain.Common.BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}

public class LessonDbContextFactory : IDesignTimeDbContextFactory<LessonDbContext>
{
    public LessonDbContext CreateDbContext(string[] args)
    {
        var basePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "src", "Lesson.Web");

        if (!File.Exists(Path.Combine(basePath, "appsettings.json")))
            basePath = Path.Combine(Directory.GetCurrentDirectory(), "src", "Lesson.Web");

        if (!File.Exists(Path.Combine(basePath, "appsettings.json")))
            basePath = Directory.GetCurrentDirectory();

        // Also try artifacts project path
        if (!File.Exists(Path.Combine(basePath, "appsettings.json")))
            basePath = Path.Combine(Directory.GetCurrentDirectory(), "Lesson.Web");

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<LessonDbContext>();
        optionsBuilder.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
        return new LessonDbContext(optionsBuilder.Options);
    }
}
