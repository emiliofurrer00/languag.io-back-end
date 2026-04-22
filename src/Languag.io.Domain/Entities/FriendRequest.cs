using Languag.io.Domain.Enums;

namespace Languag.io.Domain.Entities;

public class FriendRequest
{
    public Guid Id { get; set; }
    public Guid SenderId { get; set; }
    public User Sender { get; set; } = null!;
    public Guid ReceiverId { get; set; }
    public User Receiver { get; set; } = null!;
    public FriendRequestStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? RespondedAtUtc { get; set; }
    public Guid PairUser1Id { get; set; }
    public Guid PairUser2Id { get; set; }
}
