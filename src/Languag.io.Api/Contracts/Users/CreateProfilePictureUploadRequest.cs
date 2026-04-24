using System.ComponentModel.DataAnnotations;

namespace Languag.io.Api.Contracts.Users;

public sealed class CreateProfilePictureUploadRequest
{
    [Required]
    public string? ContentType { get; set; }

    [Range(1, 5 * 1024 * 1024)]
    public long ContentLength { get; set; }
}
