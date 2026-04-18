namespace Languag.io.Domain.Entities;

public class StudySessionResponse
{
    public Guid Id { get; set; }
    public Guid StudySessionId { get; set; }
    public StudySession StudySession { get; set; } = null!;
    public Guid DeckId { get; set; }
    public Deck Deck { get; set; } = null!;
    public Guid CardId { get; set; }
    public Card Card { get; set; } = null!;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public bool WasCorrect { get; set; }
}
