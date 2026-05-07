using System.ComponentModel.DataAnnotations;

namespace Languag.io.Domain.Entities;

public class DeckVersionCardChoice
{
    public Guid Id { get; set; }
    public Guid DeckVersionCardId { get; set; }
    public DeckVersionCard DeckVersionCard { get; set; } = null!;
    public Guid? OriginalChoiceId { get; set; }

    [Required]
    [StringLength(1000, MinimumLength = 1)]
    public string Text { get; set; } = string.Empty;

    public bool IsCorrect { get; set; }

    [Range(0, 10000)]
    public int Order { get; set; }
}
