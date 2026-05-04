using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Languag.io.Application.Audio;

public static partial class AudioHashing
{
    public static string ComputeAudioHash(
        string text,
        string languageCode,
        string provider,
        string model,
        string voice,
        decimal speed,
        string instructionsVersion)
    {
        var normalized = NormalizeForAudio(text);
        var raw = string.Join(
            '|',
            normalized,
            languageCode.Trim().ToLowerInvariant(),
            provider.Trim().ToLowerInvariant(),
            model.Trim().ToLowerInvariant(),
            voice.Trim().ToLowerInvariant(),
            speed.ToString("0.##", CultureInfo.InvariantCulture),
            instructionsVersion.Trim().ToLowerInvariant());

        return HashText(raw);
    }

    public static string ComputeInstructionsHash(string instructionsVersion)
    {
        return HashText(instructionsVersion.Trim().ToLowerInvariant());
    }

    public static string NormalizeForAudio(string text)
    {
        return WhitespaceRegex()
            .Replace(text.Trim(), " ")
            .ToLowerInvariant();
    }

    private static string HashText(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        var hash = SHA256.HashData(bytes);

        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
