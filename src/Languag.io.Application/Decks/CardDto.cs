namespace Languag.io.Api.Contracts.Decks;
public record CardDto(
    Guid Id,
    string FrontText,
    string BackText,
    int Order
);