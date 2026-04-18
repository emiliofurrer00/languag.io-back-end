namespace Languag.io.Application.StudySessions;

public sealed record SubmitStudySessionCommand(
    Guid DeckId,
    decimal PercentageCorrect,
    IReadOnlyList<SubmitStudySessionResponseCommand> Responses);

public sealed record SubmitStudySessionResponseCommand(
    Guid CardId,
    bool WasCorrect);
