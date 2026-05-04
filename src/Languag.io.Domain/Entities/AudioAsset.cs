using System.ComponentModel.DataAnnotations;

namespace Languag.io.Domain.Entities;

public class AudioAsset
{
    public Guid Id { get; set; }

    [Required]
    [StringLength(64, MinimumLength = 64)]
    public string TextHash { get; set; } = string.Empty;

    [Required]
    [StringLength(1000, MinimumLength = 1)]
    public string NormalizedText { get; set; } = string.Empty;

    [Required]
    [StringLength(20, MinimumLength = 2)]
    public string LanguageCode { get; set; } = string.Empty;

    [Required]
    [StringLength(40)]
    public string Provider { get; set; } = "openai";

    [Required]
    [StringLength(80)]
    public string Model { get; set; } = "gpt-4o-mini-tts";

    [Required]
    [StringLength(40)]
    public string Voice { get; set; } = "cedar";

    public decimal Speed { get; set; } = 0.9m;

    [Required]
    [StringLength(64, MinimumLength = 64)]
    public string InstructionsHash { get; set; } = string.Empty;

    [Required]
    [StringLength(512)]
    public string StorageKey { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    public string PublicUrl { get; set; } = string.Empty;

    public AudioAssetStatus Status { get; set; } = AudioAssetStatus.Pending;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public enum AudioAssetStatus
{
    Pending = 1,
    Processing = 2,
    Ready = 3,
    Failed = 4
}
