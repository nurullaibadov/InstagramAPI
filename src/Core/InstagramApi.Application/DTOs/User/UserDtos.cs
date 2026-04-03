namespace InstagramApi.Application.DTOs.User;

public class UserDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public string? Website { get; set; }
    public bool IsPrivate { get; set; }
    public bool IsVerified { get; set; }
    public int FollowersCount { get; set; }
    public int FollowingCount { get; set; }
    public int PostsCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsFollowing { get; set; }
    public bool IsFollowedBy { get; set; }
    public bool IsBlocked { get; set; }
}

public class UserProfileDto : UserDto
{
    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? PhoneNumber { get; set; }
    public bool EmailConfirmed { get; set; }
    public IEnumerable<string> Roles { get; set; } = new List<string>();
}

public class UpdateProfileDto
{
    public string? FullName { get; set; }
    public string? Bio { get; set; }
    public string? Website { get; set; }
    public bool? IsPrivate { get; set; }
    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? PhoneNumber { get; set; }
}

public class UserSummaryDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public bool IsVerified { get; set; }
    public bool IsFollowing { get; set; }
}
