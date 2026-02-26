namespace Languag.io.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string? Name { get; set; } = "";
    public string? Email { get; set; } = "";
    public List<Deck> Decks { get; set; } = new List<Deck>();

}
