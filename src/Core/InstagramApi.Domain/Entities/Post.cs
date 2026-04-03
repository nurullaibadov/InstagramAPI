using InstagramApi.Domain.Common;
using InstagramApi.Domain.Enums;

namespace InstagramApi.Domain.Entities;

public class Post : BaseEntity
{
    public Guid UserId { get; set; }
    public string? Caption { get; set; }
    public string? Location { get; set; }
    public PostType PostType { get; set; } = PostType.Image;
    public bool CommentsEnabled { get; set; } = true;
    public bool LikesVisible { get; set; } = true;
    public int LikesCount { get; set; } = 0;
    public int CommentsCount { get; set; } = 0;
    public int ViewsCount { get; set; } = 0;
    public PostVisibility Visibility { get; set; } = PostVisibility.Public;

    // Navigation
    public virtual AppUser User { get; set; } = null!;
    public virtual ICollection<PostMedia> MediaFiles { get; set; } = new List<PostMedia>();
    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public virtual ICollection<Like> Likes { get; set; } = new List<Like>();
    public virtual ICollection<PostHashtag> PostHashtags { get; set; } = new List<PostHashtag>();
    public virtual ICollection<PostTag> Tags { get; set; } = new List<PostTag>();
    public virtual ICollection<SavedPost> SavedByUsers { get; set; } = new List<SavedPost>();
    public virtual ICollection<Report> Reports { get; set; } = new List<Report>();
}
