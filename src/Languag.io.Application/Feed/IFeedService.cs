namespace Languag.io.Application.Feed;

public interface IFeedService
{
    Task<FeedDto?> GetFeedAsync(Guid currentUserId, CancellationToken ct = default);
}
