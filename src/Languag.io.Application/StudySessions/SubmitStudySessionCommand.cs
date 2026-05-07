namespace Languag.io.Application.StudySessions;

public sealed record SubmitStudySessionCommand(
    Guid DeckId,
    Guid? DeckVersionId,
    decimal PercentageCorrect,
    IReadOnlyList<SubmitStudySessionResponseCommand> Responses);

public sealed record SubmitStudySessionResponseCommand(
    Guid CardId,
    bool WasCorrect);
