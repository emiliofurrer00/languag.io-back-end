using Languag.io.Domain.Entities;

namespace Languag.io.Application.Decks;
public record UpdateDeckCommand (
    Guid Id,
    string Title,
    string? Description,
    string Category,
    string? Color,
    DeckVisibility Visibility);
