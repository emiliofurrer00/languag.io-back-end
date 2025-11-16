using System;
using System.Collections.Generic;
using System.Text;

namespace Languag.io.Domain.Entities
{
    public class Card
    {
        public Guid Id { get; set; }
        public Guid DeckId { get; set; }
        public string FrontText { get; set; } = string.Empty;
        public string BackText { get; set; } = string.Empty;
        public string? ExampleSentence { get; set; }
        public int Order { get; set; }
        public Deck? Deck { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
    }
}
