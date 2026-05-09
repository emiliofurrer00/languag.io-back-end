using Languag.io.Domain.Entities;

namespace Languag.io.Application.StudySessions;

public sealed class CardReviewScheduler : ICardReviewScheduler
{
    public void ApplyReviewResult(CardReviewState state, bool wasCorrect, DateTime reviewedAtUtc)
    {
        if (!wasCorrect)
        {
            state.RepetitionCount = 0;
            state.LapseCount++;
            state.IntervalDays = 1;
            state.EaseFactor = Math.Max(1.3m, state.EaseFactor - 0.2m);
        }
        else
        {
            state.RepetitionCount++;
            state.IntervalDays = state.RepetitionCount switch
            {
                1 => 1,
                2 => 3,
                _ => Math.Max(1, (int)Math.Round(state.IntervalDays * state.EaseFactor))
            };
            state.EaseFactor = Math.Min(3.0m, state.EaseFactor + 0.05m);
        }

        state.LastReviewedAtUtc = reviewedAtUtc;
        state.DueAtUtc = reviewedAtUtc.AddDays(state.IntervalDays);
        state.TotalReviews++;
        if (wasCorrect)
        {
            state.CorrectReviews++;
        }
    }
}
