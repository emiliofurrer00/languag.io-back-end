using System.ComponentModel.DataAnnotations;

namespace Languag.io.Api.Contracts.Users;

public sealed class CompleteProfilePictureUploadRequest
{
    [Required]
    public string? ObjectKey { get; set; }
}
