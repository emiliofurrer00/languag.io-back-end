namespace Languag.io.Application.Feed;

public sealed class FeedService : IFeedService
{
    private readonly IFeedRepository _feedRepository;

    public FeedService(IFeedRepository feedRepository)
    {
        _feedRepository = feedRepository;
    }

    public Task<FeedDto?> GetFeedAsync(Guid currentUserId, CancellationToken ct = default)
    {
        return _feedRepository.GetFeedAsync(currentUserId, ct);
    }
}
