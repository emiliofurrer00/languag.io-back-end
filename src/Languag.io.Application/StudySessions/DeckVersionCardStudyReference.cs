namespace Languag.io.Application.StudySessions;

public sealed record DeckVersionCardStudyReference(
    Guid DeckVersionCardId,
    Guid? OriginalCardId,
    Guid? ReviewCardId);
