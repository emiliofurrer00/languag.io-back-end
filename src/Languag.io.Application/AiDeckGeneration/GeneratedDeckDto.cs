namespace Languag.io.Application.AiDeckGeneration;

public class GeneratedDeckDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public List<GeneratedCardDto> Cards { get; set; } = [];
}

public class GeneratedCardDto
{
    public string FrontText { get; set; } = string.Empty;
    public string BackText { get; set; } = string.Empty;
    public string? ExampleSentence { get; set; }
}
