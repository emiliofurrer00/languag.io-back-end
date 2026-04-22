using System.Security.Claims;
using Languag.io.Api.Controllers;
using Languag.io.Api.Contracts.Friends;
using Languag.io.Application.Common;
using Languag.io.Application.Friends;
using Languag.io.Application.Notifications;
using Languag.io.Application.Users;
using Languag.io.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Languag.io.Tests;

public sealed class FriendsAndNotificationsControllerTests
{
    [Fact]
    public async Task FriendsController_SendFriendRequest_ReturnsUnauthorized_WhenUserIsMissing()
    {
        var controller = CreateFriendsController();

        var result = await controller.SendFriendRequest(new SendFriendRequestRequest(Guid.NewGuid()), CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task FriendsController_SendFriendRequest_ReturnsCreated_WhenRequestIsCreated()
    {
        var service = new StubFriendService
        {
            SendResult = new SendFriendRequestResult(SendFriendRequestStatus.Created, Guid.NewGuid())
        };
        var controller = CreateFriendsController(service: service, authenticated: true);

        var result = await controller.SendFriendRequest(new SendFriendRequestRequest(Guid.NewGuid()), CancellationToken.None);

        var created = Assert.IsType<CreatedResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
    }

    [Fact]
    public async Task FriendsController_SendFriendRequest_ReturnsConflict_WhenDomainConflictOccurs()
    {
        var service = new StubFriendService
        {
            SendResult = new SendFriendRequestResult(
                SendFriendRequestStatus.ReversePendingRequestExists,
                Error: "Reverse request exists.")
        };
        var controller = CreateFriendsController(service: service, authenticated: true);

        var result = await controller.SendFriendRequest(new SendFriendRequestRequest(Guid.NewGuid()), CancellationToken.None);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);
    }

    [Fact]
    public async Task FriendsController_AcceptFriendRequest_ReturnsForbidden_WhenServiceRejectsAccess()
    {
        var service = new StubFriendService
        {
            CommandResult = new FriendRequestCommandResult(FriendRequestCommandStatus.Forbidden, "Forbidden.")
        };
        var controller = CreateFriendsController(service: service, authenticated: true);

        var result = await controller.AcceptFriendRequest(Guid.NewGuid(), CancellationToken.None);

        var forbidden = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task NotificationsController_GetNotifications_ReturnsBadRequest_WhenCursorIsInvalid()
    {
        var controller = CreateNotificationsController(authenticated: true);

        var result = await controller.GetNotifications("not-a-valid-cursor", 20, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }

    [Fact]
    public async Task NotificationsController_MarkNotificationRead_ReturnsNotFound_WhenNotificationDoesNotExist()
    {
        var service = new StubNotificationService
        {
            MarkReadResult = new MarkNotificationReadResult(MarkNotificationReadStatus.NotFound)
        };
        var controller = CreateNotificationsController(service, authenticated: true);

        var result = await controller.MarkNotificationRead(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    private static FriendsController CreateFriendsController(
        StubFriendService? service = null,
        bool authenticated = false)
    {
        var controller = new FriendsController(
            service ?? new StubFriendService(),
            new StubUserIdentityService(Guid.NewGuid()));

        if (authenticated)
        {
            controller.ControllerContext = BuildControllerContext();
        }
        else
        {
            controller.ControllerContext = BuildControllerContext(authenticated: false);
        }

        return controller;
    }

    private static NotificationsController CreateNotificationsController(
        StubNotificationService? service = null,
        bool authenticated = false)
    {
        var controller = new NotificationsController(
            service ?? new StubNotificationService(),
            new StubUserIdentityService(Guid.NewGuid()));

        if (authenticated)
        {
            controller.ControllerContext = BuildControllerContext();
        }
        else
        {
            controller.ControllerContext = BuildControllerContext(authenticated: false);
        }

        return controller;
    }

    private static ControllerContext BuildControllerContext(bool authenticated = true)
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

    private sealed class StubFriendService : IFriendService
    {
        public SendFriendRequestResult SendResult { get; init; } = new(SendFriendRequestStatus.Created, Guid.NewGuid());
        public FriendRequestCommandResult CommandResult { get; init; } = new(FriendRequestCommandStatus.Success);
        public RemoveFriendResult RemoveFriendResult { get; init; } = new(RemoveFriendStatus.Success);
        public GetFriendshipStatusResult FriendshipStatusResult { get; init; } =
            new(GetFriendshipStatusResultStatus.Success, new FriendshipStatusDto(FriendshipStatuses.None));

        public Task<SendFriendRequestResult> SendFriendRequestAsync(SendFriendRequestCommand command, Guid currentUserId, CancellationToken ct = default)
        {
            return Task.FromResult(SendResult);
        }

        public Task<FriendRequestCommandResult> AcceptFriendRequestAsync(AcceptFriendRequestCommand command, Guid currentUserId, CancellationToken ct = default)
        {
            return Task.FromResult(CommandResult);
        }

        public Task<FriendRequestCommandResult> RejectFriendRequestAsync(RejectFriendRequestCommand command, Guid currentUserId, CancellationToken ct = default)
        {
            return Task.FromResult(CommandResult);
        }

        public Task<FriendRequestCommandResult> CancelFriendRequestAsync(CancelFriendRequestCommand command, Guid currentUserId, CancellationToken ct = default)
        {
            return Task.FromResult(CommandResult);
        }

        public Task<RemoveFriendResult> RemoveFriendAsync(RemoveFriendCommand command, Guid currentUserId, CancellationToken ct = default)
        {
            return Task.FromResult(RemoveFriendResult);
        }

        public Task<CursorPage<FriendRequestDto>> GetIncomingFriendRequestsAsync(GetIncomingFriendRequestsQuery query, Guid currentUserId, CancellationToken ct = default)
        {
            return Task.FromResult(new CursorPage<FriendRequestDto>([], null));
        }

        public Task<CursorPage<FriendRequestDto>> GetOutgoingFriendRequestsAsync(GetOutgoingFriendRequestsQuery query, Guid currentUserId, CancellationToken ct = default)
        {
            return Task.FromResult(new CursorPage<FriendRequestDto>([], null));
        }

        public Task<CursorPage<FriendDto>> GetFriendsAsync(GetFriendsQuery query, Guid currentUserId, CancellationToken ct = default)
        {
            return Task.FromResult(new CursorPage<FriendDto>([], null));
        }

        public Task<GetFriendshipStatusResult> GetFriendshipStatusAsync(GetFriendshipStatusQuery query, Guid currentUserId, CancellationToken ct = default)
        {
            return Task.FromResult(FriendshipStatusResult);
        }
    }

    private sealed class StubNotificationService : INotificationService
    {
        public MarkNotificationReadResult MarkReadResult { get; init; } = new(MarkNotificationReadStatus.Success);

        public Task<CursorPage<NotificationDto>> GetNotificationsAsync(GetNotificationsQuery query, Guid currentUserId, CancellationToken ct = default)
        {
            return Task.FromResult(new CursorPage<NotificationDto>([], null));
        }

        public Task<UnreadNotificationCountDto> GetUnreadNotificationCountAsync(GetUnreadNotificationCountQuery query, Guid currentUserId, CancellationToken ct = default)
        {
            return Task.FromResult(new UnreadNotificationCountDto(0));
        }

        public Task<MarkNotificationReadResult> MarkNotificationReadAsync(MarkNotificationReadCommand command, Guid currentUserId, CancellationToken ct = default)
        {
            return Task.FromResult(MarkReadResult);
        }

        public Task<int> MarkAllNotificationsReadAsync(MarkAllNotificationsReadCommand command, Guid currentUserId, CancellationToken ct = default)
        {
            return Task.FromResult(0);
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
