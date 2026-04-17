using System;
using System.Collections.Generic;
using System.Text;

namespace Languag.io.Domain.Entities
{
    public class Deck
    {
        public Guid Id { get; set; }
        public Guid? OwnerId { get; set; }
        public User? User { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Category { get; set; }
        public string? Color { get; set; }
        public DeckVisibility Visibility { get; set; } = DeckVisibility.Private;

        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
        public List<Card> Cards { get; set; } = new List<Card>();
        public List<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();
    }

    public enum DeckVisibility
    {
        Private = 0,
        Public = 1
    }
}
