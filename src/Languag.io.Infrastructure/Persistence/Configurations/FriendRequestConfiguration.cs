using Languag.io.Domain.Entities;
using Languag.io.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Languag.io.Infrastructure.Persistence.Configurations;

public sealed class FriendRequestConfiguration : IEntityTypeConfiguration<FriendRequest>
{
    public void Configure(EntityTypeBuilder<FriendRequest> builder)
    {
        builder.HasKey(friendRequest => friendRequest.Id);

        builder.Property(friendRequest => friendRequest.Status).IsRequired();
        builder.Property(friendRequest => friendRequest.CreatedAtUtc).IsRequired();
        builder.Property(friendRequest => friendRequest.PairUser1Id).IsRequired();
        builder.Property(friendRequest => friendRequest.PairUser2Id).IsRequired();

        builder.ToTable(tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "CK_FriendRequests_DistinctUsers",
                "\"SenderId\" <> \"ReceiverId\"");
        });

        builder.HasOne(friendRequest => friendRequest.Sender)
            .WithMany(user => user.SentFriendRequests)
            .HasForeignKey(friendRequest => friendRequest.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(friendRequest => friendRequest.Receiver)
            .WithMany(user => user.ReceivedFriendRequests)
            .HasForeignKey(friendRequest => friendRequest.ReceiverId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(friendRequest => new { friendRequest.ReceiverId, friendRequest.Status, friendRequest.CreatedAtUtc })
            .IsDescending(false, false, true);

        builder.HasIndex(friendRequest => new { friendRequest.SenderId, friendRequest.Status, friendRequest.CreatedAtUtc })
            .IsDescending(false, false, true);

        builder.HasIndex(friendRequest => new { friendRequest.PairUser1Id, friendRequest.PairUser2Id })
            .IsUnique()
            .HasFilter($"\"Status\" = {(int)FriendRequestStatus.Pending}");
    }
}
