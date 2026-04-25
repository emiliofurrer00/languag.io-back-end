using System.ComponentModel.DataAnnotations;

namespace Languag.io.Api.Contracts.StudySessions;

public sealed class SubmitStudySessionRequest
{
    [Range(0, 100)]
    public decimal PercentageCorrect { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(500)]
    public List<SubmitStudySessionResponseRequest> Responses { get; set; } = [];
}

public sealed class SubmitStudySessionResponseRequest
{
    public Guid CardId { get; set; }
    public bool WasCorrect { get; set; }
}
