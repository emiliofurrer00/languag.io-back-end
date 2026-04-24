using Languag.io.Api.Controllers;
using Languag.io.Application.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Languag.io.Tests;

public sealed class UsersControllerTests
{
    [Fact]
    public async Task GetPublicProfileByUsername_ReturnsNotFound_WhenProfileDoesNotExist()
    {
        var controller = CreateController(service: new StubUserProfileService());

        var result = await controller.GetPublicProfileByUsername("missing-user", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetPublicProfileByUsername_ReturnsOk_WhenProfileExists()
    {
        var expected = new PublicUserProfileDto(
            Guid.NewGuid(),
            "ada",
            "Ada Lovelace",
            "teal",
            null,
            null,
            "Linguist and builder",
            "I like language learning products.",
            true,
            DateTime.UtcNow);
        var controller = CreateController(service: new StubUserProfileService
        {
            PublicProfile = expected
        });

        var result = await controller.GetPublicProfileByUsername(expected.Username, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expected, ok.Value);
    }

    private static UsersController CreateController(StubUserProfileService? service = null)
    {
        return new UsersController(
            new StubUserIdentityService(),
            service ?? new StubUserProfileService(),
            new StubProfilePictureService())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    private sealed class StubUserProfileService : IUserProfileService
    {
        public PublicUserProfileDto? PublicProfile { get; init; }

        public Task<UserProfileDto?> GetByIdAsync(Guid userId, CancellationToken ct = default)
        {
            return Task.FromResult<UserProfileDto?>(null);
        }

        public Task<PublicUserProfileDto?> GetPublicByUsernameAsync(string username, CancellationToken ct = default)
        {
            return Task.FromResult(PublicProfile);
        }

        public Task<bool> IsUsernameAvailableAsync(string username, Guid excludingUserId, CancellationToken ct = default)
        {
            return Task.FromResult(true);
        }

        public Task<UpdateUserProfileResult> UpdateAsync(UpdateUserProfileCommand command, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class StubUserIdentityService : IUserIdentityService
    {
        public Task<Guid> GetOrCreateUserIdAsync(AuthenticatedUser user, CancellationToken ct = default)
        {
            return Task.FromResult(Guid.NewGuid());
        }
    }

    private sealed class StubProfilePictureService : IProfilePictureService
    {
        public Task<CreateProfilePictureUploadResult> CreateUploadAsync(
            Guid userId,
            string? contentType,
            long contentLength,
            CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<CompleteProfilePictureUploadResult> CompleteUploadAsync(
            Guid userId,
            string? objectKey,
            CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }
    }
}
