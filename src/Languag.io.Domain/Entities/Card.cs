using System.ComponentModel.DataAnnotations;

namespace Languag.io.Domain.Entities
{
    public class Card
    {
        public Guid Id { get; set; }
        public Guid DeckId { get; set; }

        [Required]
        [StringLength(1000, MinimumLength = 1)]
        public string FrontText { get; set; } = string.Empty;

        [Required]
        [StringLength(1000, MinimumLength = 1)]
        public string BackText { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? ExampleSentence { get; set; }

        [Range(0, 10000)]
        public int Order { get; set; }

        public Deck? Deck { get; set; }
        public List<StudySessionResponse> StudySessionResponses { get; set; } = [];
        public List<CardReviewState> ReviewStates { get; set; } = [];
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
    }
}
