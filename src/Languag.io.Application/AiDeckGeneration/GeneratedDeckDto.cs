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
    public string Type { get; set; } = Languag.io.Domain.Entities.CardTypes.Flashcard;
    public string FrontText { get; set; } = string.Empty;
    public string BackText { get; set; } = string.Empty;
    public string? TtsText { get; set; }
    public string? ExampleSentence { get; set; }
    public List<GeneratedCardChoiceDto> Choices { get; set; } = [];
}

public class GeneratedCardChoiceDto
{
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}
