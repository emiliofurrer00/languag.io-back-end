namespace Languag.io.Application.StudySessions;

public sealed record DeckStudyVersionReference(
    Guid DeckVersionId,
    int VersionNumber);
