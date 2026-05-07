using System.ComponentModel.DataAnnotations;

namespace Languag.io.Domain.Entities;

public class AiSagaGenerationJob
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

    [Range(2, 6)]
    public int RequestedDeckCount { get; set; }

    [Range(5, 15)]
    public int RequestedCardsPerDeck { get; set; }

    [Range(0, 15)]
    public int RequestedMultiChoiceCountPerDeck { get; set; }

    public bool IncludeAudio { get; set; }

    public AiSagaGenerationStatus Status { get; set; } = AiSagaGenerationStatus.Pending;

    public AiSagaAudioStatus AudioStatus { get; set; } = AiSagaAudioStatus.NotRequested;

    public Guid? CreatedSagaId { get; set; }
    public Saga? CreatedSaga { get; set; }

    [StringLength(2000)]
    public string? ErrorMessage { get; set; }

    public int RetryCount { get; set; }

    public DateTime UsageWeekStartUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}

public enum AiSagaGenerationStatus
{
    Pending = 1,
    Processing = 2,
    Completed = 3,
    Failed = 4
}

public enum AiSagaAudioStatus
{
    NotRequested = 1,
    Pending = 2,
    Processing = 3,
    Ready = 4,
    Failed = 5
}
