using System.ComponentModel.DataAnnotations;
using Languag.io.Domain.Entities;

namespace Languag.io.Api.Contracts.Decks;

public class UpdateDeckRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [StringLength(80)]
    public string Category { get; set; } = string.Empty;

    [StringLength(20)]
    public string Color { get; set; } = "teal";

    public DeckVisibility Visibility { get; set; } = DeckVisibility.Private;

    [Required]
    [MinLength(1)]
    [MaxLength(200)]
    public List<Card> Cards { get; set; } = [];
}
