using System.ComponentModel.DataAnnotations;

namespace Languag.io.Api.Contracts.Users;

public sealed class UpdateUserProfileRequest
{
    [StringLength(50)]
    public string? Username { get; set; }

    [StringLength(100)]
    public string? Name { get; set; }

    public bool HasBeenOnboarded { get; set; }

    [Range(0, int.MaxValue)]
    public int DailyCardsGoal { get; set; }

    [StringLength(20)]
    public string AvatarColor { get; set; } = "teal";

    [StringLength(280)]
    public string ProfileDescription { get; set; } = string.Empty;

    [StringLength(2000)]
    public string About { get; set; } = string.Empty;

    public bool IsPublicProfile { get; set; }
}
