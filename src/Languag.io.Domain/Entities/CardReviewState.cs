namespace Languag.io.Domain.Entities;

public class CardReviewState
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid DeckId { get; set; }
    public Deck Deck { get; set; } = null!;
    public Guid CardId { get; set; }
    public Card Card { get; set; } = null!;
    public DateTime? LastReviewedAtUtc { get; set; }
    public DateTime DueAtUtc { get; set; }
    public int IntervalDays { get; set; }
    public decimal EaseFactor { get; set; } = 2.5m;
    public int RepetitionCount { get; set; }
    public int LapseCount { get; set; }
    public int TotalReviews { get; set; }
    public int CorrectReviews { get; set; }
}
