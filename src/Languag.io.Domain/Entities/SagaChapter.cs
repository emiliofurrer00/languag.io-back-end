using System.ComponentModel.DataAnnotations;

namespace Languag.io.Domain.Entities;

public class SagaChapter
{
    public Guid Id { get; set; }
    public Guid SagaId { get; set; }
    public Saga Saga { get; set; } = null!;

    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Range(0, 10000)]
    public int Order { get; set; }

    public List<SagaLesson> Lessons { get; set; } = [];
}
