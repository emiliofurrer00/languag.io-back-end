namespace Languag.io.Domain.Entities;

public class ActivityLog
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid? DeckId { get; set; }
    public Deck? Deck { get; set; }
    public ActivityType Type { get; set; }
    public int? StreakDays { get; set; }
    public string? Metadata { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public enum ActivityType
{
    DeckCreated = 1,
    DeckStudySessionCompleted = 2,
    DeckMastered = 3,
    DayStreakReached = 4,
    FirstDeckCreated = 5,
    FirstStudySessionCompleted = 6
}
