namespace Languag.io.Application.Feed;

public interface IFeedRepository
{
    Task<FeedDto?> GetFeedAsync(Guid currentUserId, CancellationToken ct = default);
}
