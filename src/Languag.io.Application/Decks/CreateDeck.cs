using Languag.io.Domain.Entities;

namespace Languag.io.Application.Decks;

public record CreateDeckCommand(
    string Title,
    string? Description,
    string? LanguageCode,
    DeckVisibility Visibility);