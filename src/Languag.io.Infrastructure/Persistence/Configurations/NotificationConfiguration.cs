using Languag.io.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Languag.io.Infrastructure.Persistence.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(notification => notification.Id);

        builder.Property(notification => notification.Type).IsRequired();
        builder.Property(notification => notification.EntityType).HasMaxLength(100);
        builder.Property(notification => notification.Title).HasMaxLength(200);
        builder.Property(notification => notification.Body).HasMaxLength(1000);
        builder.Property(notification => notification.CreatedAtUtc).IsRequired();
        builder.Property(notification => notification.IsRead).HasDefaultValue(false);

        builder.HasOne(notification => notification.User)
            .WithMany(user => user.Notifications)
            .HasForeignKey(notification => notification.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(notification => notification.ActorUser)
            .WithMany(user => user.AuthoredNotifications)
            .HasForeignKey(notification => notification.ActorUserId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.HasIndex(notification => new { notification.UserId, notification.IsRead, notification.CreatedAtUtc, notification.Id })
            .IsDescending(false, false, true, true);

        builder.HasIndex(notification => new { notification.UserId, notification.CreatedAtUtc, notification.Id })
            .IsDescending(false, true, true);
    }
}
