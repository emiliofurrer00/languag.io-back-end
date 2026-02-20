using Languag.io.Domain.Entities;

namespace Languag.io.Api.Contracts.Decks;
public record DeckDto(
    Guid Id,
    string Title,
    string Category,
    string? Description,
    DeckVisibility Visibility,
    string? Color,
    List<CardDto> Cards
);