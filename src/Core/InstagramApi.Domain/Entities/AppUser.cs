using InstagramApi.Domain.Common;
using Microsoft.AspNetCore.Identity;

namespace InstagramApi.Domain.Entities;

public class AppUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public string? Website { get; set; }
    public bool IsPrivate { get; set; } = false;
    public bool IsVerified { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiry { get; set; }
    public string? EmailConfirmationToken { get; set; }
    public int FollowersCount { get; set; } = 0;
    public int FollowingCount { get; set; } = 0;
    public int PostsCount { get; set; } = 0;
    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? PhoneNumber2 { get; set; }

    // Navigation
    public virtual ICollection<Post> Posts { get; set; } = new List<Post>();
    public virtual ICollection<Story> Stories { get; set; } = new List<Story>();
    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public virtual ICollection<Like> Likes { get; set; } = new List<Like>();
    public virtual ICollection<Follow> Followers { get; set; } = new List<Follow>();
    public virtual ICollection<Follow> Following { get; set; } = new List<Follow>();
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public virtual ICollection<Message> SentMessages { get; set; } = new List<Message>();
    public virtual ICollection<Message> ReceivedMessages { get; set; } = new List<Message>();
    public virtual ICollection<SavedPost> SavedPosts { get; set; } = new List<SavedPost>();
    public virtual ICollection<BlockedUser> BlockedUsers { get; set; } = new List<BlockedUser>();
    public virtual ICollection<BlockedUser> BlockedByUsers { get; set; } = new List<BlockedUser>();
    public virtual ICollection<Report> Reports { get; set; } = new List<Report>();
    public virtual ICollection<HashtagFollow> HashtagFollows { get; set; } = new List<HashtagFollow>();
}
