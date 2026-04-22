using Languag.io.Domain.Enums;

namespace Languag.io.Domain.Entities;

public class Notification
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public NotificationType Type { get; set; }
    public Guid? ActorUserId { get; set; }
    public User? ActorUser { get; set; }
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public string? Title { get; set; }
    public string? Body { get; set; }
    public string? PayloadJson { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
