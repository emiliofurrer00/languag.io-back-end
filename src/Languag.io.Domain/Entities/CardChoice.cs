using System.ComponentModel.DataAnnotations;

namespace Languag.io.Domain.Entities
{
    public class CardChoice
    {
        public Guid Id { get; set; }
        public Guid CardId { get; set; }

        [Required]
        [StringLength(1000, MinimumLength = 1)]
        public string Text { get; set; } = string.Empty;

        public bool IsCorrect { get; set; }

        [Range(0, 10000)]
        public int Order { get; set; }

        public Card? Card { get; set; }
    }
}
