using System.ComponentModel.DataAnnotations;

namespace Languag.io.Domain.Entities;

public class DeckVersionCard
{
    public Guid Id { get; set; }
    public Guid DeckVersionId { get; set; }
    public DeckVersion DeckVersion { get; set; } = null!;
    public Guid? OriginalCardId { get; set; }

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

    public Guid? FrontAudioAssetId { get; set; }
    public AudioAsset? FrontAudioAsset { get; set; }
    public List<DeckVersionCardChoice> Choices { get; set; } = [];
    public List<StudySessionResponse> StudySessionResponses { get; set; } = [];
}
