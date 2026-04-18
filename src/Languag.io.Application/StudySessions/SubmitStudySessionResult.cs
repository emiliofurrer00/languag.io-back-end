namespace Languag.io.Application.StudySessions;

public sealed record SubmitStudySessionResult(
    SubmitStudySessionStatus Status,
    Guid? StudySessionId = null,
    string? Error = null);

public enum SubmitStudySessionStatus
{
    Created = 1,
    DeckNotFound = 2,
    Invalid = 3
}
