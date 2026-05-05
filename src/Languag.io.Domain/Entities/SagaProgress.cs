namespace Languag.io.Domain.Entities;

public class SagaProgress
{
    public Guid SagaId { get; set; }
    public Saga Saga { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid? LastStudiedLessonId { get; set; }
    public SagaLesson? LastStudiedLesson { get; set; }

    public Guid? HighestCompletedLessonId { get; set; }
    public SagaLesson? HighestCompletedLesson { get; set; }

    public DateTime StartedAtUtc { get; set; }
    public DateTime LastStudiedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}
