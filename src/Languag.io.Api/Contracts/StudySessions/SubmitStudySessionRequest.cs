namespace Languag.io.Api.Contracts.StudySessions;

public sealed class SubmitStudySessionRequest
{
    public decimal PercentageCorrect { get; set; }
    public List<SubmitStudySessionResponseRequest> Responses { get; set; } = [];
}

public sealed class SubmitStudySessionResponseRequest
{
    public Guid CardId { get; set; }
    public bool WasCorrect { get; set; }
}
