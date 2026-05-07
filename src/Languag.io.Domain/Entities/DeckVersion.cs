using System.ComponentModel.DataAnnotations;

namespace Languag.io.Domain.Entities;

public class DeckVersion
{
    public Guid Id { get; set; }
    public Guid DeckId { get; set; }
    public Deck Deck { get; set; } = null!;

    [Range(1, int.MaxValue)]
    public int VersionNumber { get; set; }

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
    public Guid? CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public List<DeckVersionCard> Cards { get; set; } = [];
    public List<StudySession> StudySessions { get; set; } = [];

    public static DeckVersion CreateSnapshot(
        Deck deck,
        IEnumerable<Card> cards,
        int versionNumber,
        DateTime createdAtUtc,
        Guid? createdByUserId)
    {
        var version = new DeckVersion
        {
            Id = Guid.NewGuid(),
            DeckId = deck.Id,
            VersionNumber = versionNumber,
            Title = deck.Title,
            Description = deck.Description,
            Category = deck.Category,
            Color = deck.Color,
            Visibility = deck.Visibility,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = createdAtUtc
        };

        version.Cards = cards
            .OrderBy(card => card.Order)
            .Select(card => CreateSnapshotCard(card, version.Id))
            .ToList();

        return version;
    }

    private static DeckVersionCard CreateSnapshotCard(Card card, Guid deckVersionId)
    {
        var versionCardId = Guid.NewGuid();
        return new DeckVersionCard
        {
            Id = versionCardId,
            DeckVersionId = deckVersionId,
            OriginalCardId = card.Id,
            Type = card.Type,
            FrontText = card.FrontText,
            BackText = card.BackText,
            ExampleSentence = card.ExampleSentence,
            Order = card.Order,
            FrontAudioAssetId = card.FrontAudioAssetId,
            Choices = card.Choices
                .OrderBy(choice => choice.Order)
                .Select(choice => new DeckVersionCardChoice
                {
                    Id = Guid.NewGuid(),
                    DeckVersionCardId = versionCardId,
                    OriginalChoiceId = choice.Id,
                    Text = choice.Text,
                    IsCorrect = choice.IsCorrect,
                    Order = choice.Order
                })
                .ToList()
        };
    }
}
