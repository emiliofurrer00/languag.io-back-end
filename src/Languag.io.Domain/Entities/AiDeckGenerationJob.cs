using System.ComponentModel.DataAnnotations;

namespace Languag.io.Domain.Entities;

public class AiDeckGenerationJob
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }

    [Required]
    [StringLength(1000, MinimumLength = 1)]
    public string Prompt { get; set; } = string.Empty;

    [StringLength(80)]
    public string? TargetLanguage { get; set; }

    [StringLength(80)]
    public string? NativeLanguage { get; set; }

    [Required]
    [StringLength(40)]
    public string Difficulty { get; set; } = "Beginner";

    [Range(5, 20)]
    public int RequestedCardCount { get; set; }

    public AiDeckGenerationStatus Status { get; set; } = AiDeckGenerationStatus.Pending;

    public Guid? CreatedDeckId { get; set; }
    public Deck? CreatedDeck { get; set; }

    [StringLength(2000)]
    public string? ErrorMessage { get; set; }

    public int RetryCount { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}

public enum AiDeckGenerationStatus
{
    Pending = 1,
    Processing = 2,
    Completed = 3,
    Failed = 4
}
