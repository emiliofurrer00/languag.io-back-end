using Languag.io.Application.StudySessions;
using Languag.io.Domain.Entities;

namespace Languag.io.Tests;

public sealed class CardReviewSchedulerTests
{
    private static readonly DateTime ReviewedAtUtc = new(2026, 5, 9, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ApplyReviewResult_SchedulesFirstCorrectReviewForTomorrow()
    {
        var scheduler = new CardReviewScheduler();
        var state = new CardReviewState { EaseFactor = 2.5m };

        scheduler.ApplyReviewResult(state, wasCorrect: true, ReviewedAtUtc);

        Assert.Equal(1, state.RepetitionCount);
        Assert.Equal(1, state.IntervalDays);
        Assert.Equal(2.55m, state.EaseFactor);
        Assert.Equal(1, state.TotalReviews);
        Assert.Equal(1, state.CorrectReviews);
        Assert.Equal(ReviewedAtUtc, state.LastReviewedAtUtc);
        Assert.Equal(ReviewedAtUtc.AddDays(1), state.DueAtUtc);
    }

    [Fact]
    public void ApplyReviewResult_UsesEaseAfterSecondCorrectReview()
    {
        var scheduler = new CardReviewScheduler();
        var state = new CardReviewState
        {
            EaseFactor = 2.5m,
            IntervalDays = 3,
            RepetitionCount = 2,
            TotalReviews = 2,
            CorrectReviews = 2
        };

        scheduler.ApplyReviewResult(state, wasCorrect: true, ReviewedAtUtc);

        Assert.Equal(3, state.RepetitionCount);
        Assert.Equal(8, state.IntervalDays);
        Assert.Equal(2.55m, state.EaseFactor);
        Assert.Equal(3, state.TotalReviews);
        Assert.Equal(3, state.CorrectReviews);
        Assert.Equal(ReviewedAtUtc.AddDays(8), state.DueAtUtc);
    }

    [Fact]
    public void ApplyReviewResult_ResetsRepetitionAndFloorsEaseWhenIncorrect()
    {
        var scheduler = new CardReviewScheduler();
        var state = new CardReviewState
        {
            EaseFactor = 1.35m,
            IntervalDays = 10,
            RepetitionCount = 4,
            TotalReviews = 4,
            CorrectReviews = 4
        };

        scheduler.ApplyReviewResult(state, wasCorrect: false, ReviewedAtUtc);

        Assert.Equal(0, state.RepetitionCount);
        Assert.Equal(1, state.LapseCount);
        Assert.Equal(1, state.IntervalDays);
        Assert.Equal(1.3m, state.EaseFactor);
        Assert.Equal(5, state.TotalReviews);
        Assert.Equal(4, state.CorrectReviews);
        Assert.Equal(ReviewedAtUtc.AddDays(1), state.DueAtUtc);
    }
}
