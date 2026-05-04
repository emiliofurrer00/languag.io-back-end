namespace Languag.io.Infrastructure.Audio;

public sealed class OpenAiTextToSpeechOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4o-mini-tts";
    public string Voice { get; set; } = "cedar";
    public decimal Speed { get; set; } = 0.9m;
    public string InstructionsVersion { get; set; } = "language-learning-v1";
}
