using Lesson.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lesson.Infrastructure.Persistence.Configurations;

public sealed class AuthenticationSessionConfiguration : IEntityTypeConfiguration<AuthenticationSession>
{
    public void Configure(EntityTypeBuilder<AuthenticationSession> builder)
    {
        builder.ToTable("AuthenticationSessions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SessionKey).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => new { x.UserId, x.SessionKey }).IsUnique();
        builder.HasIndex(x => x.UserDeviceId);
        builder.HasOne(x => x.User).WithMany(x => x.AuthenticationSessions).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.UserDevice).WithMany(x => x.AuthenticationSessions).HasForeignKey(x => x.UserDeviceId).OnDelete(DeleteBehavior.Cascade);
    }
}
