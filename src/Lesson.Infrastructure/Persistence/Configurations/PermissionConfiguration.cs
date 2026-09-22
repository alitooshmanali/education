using Lesson.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lesson.Infrastructure.Persistence.Configurations;

public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.UserId, x.FolderId }).IsUnique();
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.FolderId);
        builder.HasOne(x => x.User).WithMany(x => x.Permissions).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Folder).WithMany(x => x.Permissions).HasForeignKey(x => x.FolderId).OnDelete(DeleteBehavior.Cascade);
    }
}
