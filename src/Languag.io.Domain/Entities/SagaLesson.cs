using System.ComponentModel.DataAnnotations;

namespace Languag.io.Domain.Entities;

public class SagaLesson
{
    public Guid Id { get; set; }
    public Guid SagaChapterId { get; set; }
    public SagaChapter SagaChapter { get; set; } = null!;

    public Guid DeckId { get; set; }
    public Deck Deck { get; set; } = null!;

    [StringLength(200)]
    public string? Title { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }

    [Range(0, 10000)]
    public int Order { get; set; }
}
