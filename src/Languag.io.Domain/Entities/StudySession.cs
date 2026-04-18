namespace Languag.io.Domain.Entities;

public class StudySession
{
    public Guid Id { get; set; }
    public Guid DeckId { get; set; }
    public Deck Deck { get; set; } = null!;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; }
    public decimal PercentageCorrect { get; set; }
    public List<StudySessionResponse> Responses { get; set; } = new();
}
