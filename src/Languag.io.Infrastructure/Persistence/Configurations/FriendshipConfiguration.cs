using Languag.io.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Languag.io.Infrastructure.Persistence.Configurations;

public sealed class FriendshipConfiguration : IEntityTypeConfiguration<Friendship>
{
    public void Configure(EntityTypeBuilder<Friendship> builder)
    {
        builder.HasKey(friendship => new { friendship.User1Id, friendship.User2Id });

        builder.Property(friendship => friendship.CreatedAtUtc).IsRequired();

        builder.HasOne(friendship => friendship.User1)
            .WithMany(user => user.FriendshipsAsUser1)
            .HasForeignKey(friendship => friendship.User1Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(friendship => friendship.User2)
            .WithMany(user => user.FriendshipsAsUser2)
            .HasForeignKey(friendship => friendship.User2Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(friendship => friendship.CreatedFromRequest)
            .WithMany()
            .HasForeignKey(friendship => friendship.CreatedFromRequestId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(friendship => new { friendship.User1Id, friendship.CreatedAtUtc, friendship.User2Id })
            .IsDescending(false, true, true);

        builder.HasIndex(friendship => new { friendship.User2Id, friendship.CreatedAtUtc, friendship.User1Id })
            .IsDescending(false, true, true);
    }
}
