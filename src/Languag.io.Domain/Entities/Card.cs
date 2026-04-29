using System.ComponentModel.DataAnnotations;

namespace Languag.io.Domain.Entities
{
    public class Card : IValidatableObject
    {
        public Guid Id { get; set; }
        public Guid DeckId { get; set; }

        [Required]
        [StringLength(40, MinimumLength = 1)]
        public string Type { get; set; } = CardTypes.Flashcard;

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
        public List<CardChoice> Choices { get; set; } = [];
        public List<StudySessionResponse> StudySessionResponses { get; set; } = [];
        public List<CardReviewState> ReviewStates { get; set; } = [];
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!CardTypes.IsSupported(Type))
            {
                yield return new ValidationResult(
                    $"Card type must be '{CardTypes.Flashcard}' or '{CardTypes.MultiChoice}'.",
                    [nameof(Type)]);
            }

            if (string.Equals(Type, CardTypes.MultiChoice, StringComparison.OrdinalIgnoreCase))
            {
                var choices = Choices ?? [];

                if (choices.Count < 2)
                {
                    yield return new ValidationResult(
                        "Multi-choice cards must include at least two choices.",
                        [nameof(Choices)]);
                }

                if (choices.Count(choice => choice.IsCorrect) != 1)
                {
                    yield return new ValidationResult(
                        "Multi-choice cards must include exactly one correct choice.",
                        [nameof(Choices)]);
                }
            }
        }
    }

    public static class CardTypes
    {
        public const string Flashcard = "flashcard";
        public const string MultiChoice = "multi-choice";

        public static bool IsSupported(string? type)
        {
            return string.Equals(type, Flashcard, StringComparison.OrdinalIgnoreCase)
                || string.Equals(type, MultiChoice, StringComparison.OrdinalIgnoreCase);
        }

        public static string Normalize(string? type)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                return Flashcard;
            }

            if (string.Equals(type, MultiChoice, StringComparison.OrdinalIgnoreCase))
            {
                return MultiChoice;
            }

            if (string.Equals(type, Flashcard, StringComparison.OrdinalIgnoreCase))
            {
                return Flashcard;
            }

            return type.Trim();
        }
    }
}
