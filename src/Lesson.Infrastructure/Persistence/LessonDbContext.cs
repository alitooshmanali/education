using Lesson.Domain.Entities;
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
    public DbSet<ContentProgress> ContentProgresses => Set<ContentProgress>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserDevice> UserDevices => Set<UserDevice>();
    public DbSet<AuthenticationSession> AuthenticationSessions => Set<AuthenticationSession>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LessonDbContext).Assembly);

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
                .OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(x => new { x.UserId, x.ContentId }).IsUnique();
        });

        modelBuilder.Entity<Folder>(b =>
        {
            b.ToTable("Folders");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.RelativePath).HasMaxLength(1000).IsRequired();
            b.HasIndex(x => x.ParentFolderId);
            b.HasOne(x => x.ParentFolder)
                .WithMany(x => x.ChildFolders)
                .HasForeignKey(x => x.ParentFolderId)
                .OnDelete(DeleteBehavior.Restrict);
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
