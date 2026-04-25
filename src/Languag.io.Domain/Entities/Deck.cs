using System.ComponentModel.DataAnnotations;

namespace Languag.io.Domain.Entities
{
    public class Deck
    {
        public Guid Id { get; set; }
        public Guid? OwnerId { get; set; }
        public User? User { get; set; }

        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [StringLength(80)]
        public string? Category { get; set; }

        [StringLength(20)]
        public string? Color { get; set; }

        public DeckVisibility Visibility { get; set; } = DeckVisibility.Private;

        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
        public List<Card> Cards { get; set; } = [];
        public List<ActivityLog> ActivityLogs { get; set; } = [];
        public List<StudySession> StudySessions { get; set; } = [];
        public List<StudySessionResponse> StudySessionResponses { get; set; } = [];
    }

    public enum DeckVisibility
    {
        Private = 0,
        Public = 1
    }
}
