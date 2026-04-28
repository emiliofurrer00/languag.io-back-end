namespace Languag.io.Application.Decks;

public sealed class DeckListQuery
{
    public string? SearchQuery { get; init; }
    public string? Search { get; init; }
    public string? Q { get; init; }
    public string? Username { get; init; }
    public string? Owner { get; init; }

    public string? NormalizedSearchQuery => FirstTrimmed(SearchQuery, Search, Q);
    public string? NormalizedOwnerUsername => FirstTrimmed(Username, Owner);

    private static string? FirstTrimmed(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }
}
