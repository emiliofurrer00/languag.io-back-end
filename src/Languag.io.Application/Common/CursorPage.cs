namespace Languag.io.Application.Common;

public sealed record CursorPage<T>(
    IReadOnlyList<T> Items,
    string? NextCursor);
