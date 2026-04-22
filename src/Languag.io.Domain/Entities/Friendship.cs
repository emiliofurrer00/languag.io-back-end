namespace Languag.io.Domain.Entities;

public class Friendship
{
    public Guid User1Id { get; set; }
    public User User1 { get; set; } = null!;
    public Guid User2Id { get; set; }
    public User User2 { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; }
    public Guid? CreatedFromRequestId { get; set; }
    public FriendRequest? CreatedFromRequest { get; set; }
}
