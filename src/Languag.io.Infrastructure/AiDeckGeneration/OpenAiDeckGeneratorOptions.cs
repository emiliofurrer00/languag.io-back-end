namespace Languag.io.Infrastructure.AiDeckGeneration;

public class OpenAiDeckGeneratorOptions
{
    public string? ApiKey { get; set; }
    public string Model { get; set; } = "gpt-5-mini";
}
