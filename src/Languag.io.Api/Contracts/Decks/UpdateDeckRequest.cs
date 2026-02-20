using Languag.io.Domain.Entities;

namespace Languag.io.Api.Contracts.Decks
{
    public class UpdateDeckRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Color { get; set; } = "teal";
        public DeckVisibility Visibility { get; set; } = DeckVisibility.Private;
        public List<Card> Cards { get; set; } = [];
    }
}
