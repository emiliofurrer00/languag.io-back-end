using System.Security.Claims;
using Languag.io.Api.Auth;
using Languag.io.Api.Controllers;
using Languag.io.Application.Feed;
using Languag.io.Application.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Languag.io.Tests;

public sealed class FeedControllerTests
{
    [Fact]
    public async Task GetFeed_ReturnsUnauthorized_WhenUserIsMissing()
    {
        var controller = CreateController();

        var result = await controller.GetFeed(CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task GetFeed_ReturnsOk_WhenFeedExists()
    {
        var controller = CreateController(
            new StubFeedService
            {
                Feed = new FeedDto(
                    new FeedDailyGoalDto(20, 10, 50),
                    new FeedStreakDto(2, []),
                    new FeedSummaryDto(null, 3, 18),
                    [],
                    [],
                    [],
                    [])
            },
            authenticated: true);

        var result = await controller.GetFeed(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var feed = Assert.IsType<FeedDto>(ok.Value);
        Assert.Equal(20, feed.DailyGoal.Goal);
        Assert.Equal(3, feed.Summary.Decks);
    }

    private static FeedController CreateController(
        StubFeedService? service = null,
        bool authenticated = false)
    {
        var controller = new FeedController(
            service ?? new StubFeedService(),
            new StubUserIdentityService(Guid.NewGuid()))
        {
            ControllerContext = BuildControllerContext(authenticated)
        };

        return controller;
    }

    private static ControllerContext BuildControllerContext(bool authenticated)
    {
        ClaimsPrincipal claimsPrincipal;
        if (authenticated)
        {
            claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("sub", "kp_test_user"),
                new Claim("email", "test@example.com"),
                new Claim("name", "Test User")
            ],
            "Bearer"));
        }
        else
        {
            claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity());
        }

        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            }
        };
    }

    private sealed class StubFeedService : IFeedService
    {
        public FeedDto? Feed { get; init; }

        public Task<FeedDto?> GetFeedAsync(Guid currentUserId, CancellationToken ct = default)
        {
            return Task.FromResult(Feed);
        }
    }

    private sealed class StubUserIdentityService : IUserIdentityService
    {
        private readonly Guid _userId;

        public StubUserIdentityService(Guid userId)
        {
            _userId = userId;
        }

        public Task<Guid> GetOrCreateUserIdAsync(AuthenticatedUser user, CancellationToken ct = default)
        {
            return Task.FromResult(_userId);
        }
    }
}
