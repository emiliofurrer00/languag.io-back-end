using Languag.io.Domain.Entities;

namespace Languag.io.Application.StudySessions;

public interface ICardReviewScheduler
{
    void ApplyReviewResult(CardReviewState state, bool wasCorrect, DateTime reviewedAtUtc);
}
