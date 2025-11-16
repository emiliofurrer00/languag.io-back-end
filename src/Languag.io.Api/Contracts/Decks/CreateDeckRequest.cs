using Languag.io.Domain.Entities;

namespace Languag.io.Api.Contracts.Decks;

public class CreateDeckRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LanguageCode { get; set; }
    public DeckVisibility Visibility { get; set; } = DeckVisibility.Private;
}