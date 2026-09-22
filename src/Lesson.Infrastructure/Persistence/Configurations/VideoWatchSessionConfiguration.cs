using Lesson.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lesson.Infrastructure.Persistence.Configurations;

public sealed class VideoWatchSessionConfiguration : IEntityTypeConfiguration<VideoWatchSession>
{
    public void Configure(EntityTypeBuilder<VideoWatchSession> builder)
    {
        builder.ToTable("VideoWatchSessions");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.UserDeviceId);
        builder.HasIndex(x => x.VideoId);

        builder.HasOne(x => x.User)
            .WithMany(x => x.VideoWatchSessions)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Video)
            .WithMany(x => x.WatchSessions)
            .HasForeignKey(x => x.VideoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.UserDevice)
            .WithMany(x => x.VideoWatchSessions)
            .HasForeignKey(x => x.UserDeviceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.AuthenticationSession)
            .WithMany()
            .HasForeignKey(x => x.AuthenticationSessionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
