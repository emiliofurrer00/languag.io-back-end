namespace Languag.io.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string? Name { get; set; } = "";
    public string? Email { get; set; } = "";
    public string? Username { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public bool HasBeenOnboarded { get; set; }
    public int DailyCardsGoal { get; set; }
    public string AvatarColor { get; set; } = "teal";
    public string ProfileDescription { get; set; } = string.Empty;
    public string About { get; set; } = string.Empty;
    public bool IsPublicProfile { get; set; }
    public List<Deck> Decks { get; set; } = new List<Deck>();
    public List<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();
    public List<StudySession> StudySessions { get; set; } = new List<StudySession>();
    public List<StudySessionResponse> StudySessionResponses { get; set; } = new List<StudySessionResponse>();

}
