using Languag.io.Application.AiDeckGeneration;

namespace Languag.io.Application.AiSagaGeneration;

public class GeneratedSagaDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public List<GeneratedSagaChapterDto> Chapters { get; set; } = [];
}

public class GeneratedSagaChapterDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<GeneratedSagaLessonDto> Lessons { get; set; } = [];
}

public class GeneratedSagaLessonDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public GeneratedDeckDto Deck { get; set; } = new();
}
