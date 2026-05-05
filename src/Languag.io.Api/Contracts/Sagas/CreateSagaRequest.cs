using System.ComponentModel.DataAnnotations;
using Languag.io.Domain.Entities;

namespace Languag.io.Api.Contracts.Sagas;

public sealed class CreateSagaRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [StringLength(80)]
    public string Category { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Color { get; set; } = "teal";

    public DeckVisibility Visibility { get; set; } = DeckVisibility.Private;

    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public List<CreateSagaChapterRequest> Chapters { get; set; } = [];
}

public sealed class CreateSagaChapterRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Range(0, 10000)]
    public int Order { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(200)]
    public List<CreateSagaLessonRequest> Lessons { get; set; } = [];
}

public sealed class CreateSagaLessonRequest
{
    public Guid DeckId { get; set; }

    [StringLength(200)]
    public string? Title { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }

    [Range(0, 10000)]
    public int Order { get; set; }
}
