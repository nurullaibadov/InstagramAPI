using InstagramApi.Domain.Common;

namespace InstagramApi.Domain.Entities;

public class Like : BaseEntity
{
    public Guid PostId { get; set; }
    public Guid UserId { get; set; }

    public virtual Post Post { get; set; } = null!;
    public virtual AppUser User { get; set; } = null!;
}

public class CommentLike : BaseEntity
{
    public Guid CommentId { get; set; }
    public Guid UserId { get; set; }

    public virtual Comment Comment { get; set; } = null!;
    public virtual AppUser User { get; set; } = null!;
}

public class Follow : BaseEntity
{
    public Guid FollowerId { get; set; }
    public Guid FollowingId { get; set; }
    public bool IsAccepted { get; set; } = true; // false if private account

    public virtual AppUser Follower { get; set; } = null!;
    public virtual AppUser Following { get; set; } = null!;
}

public class Story : BaseEntity
{
    public Guid UserId { get; set; }
    public string MediaUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string MediaType { get; set; } = "image";
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(24);
    public int ViewsCount { get; set; } = 0;
    public string? Caption { get; set; }
    public string? Location { get; set; }
    public bool IsHighlighted { get; set; } = false;

    public virtual AppUser User { get; set; } = null!;
    public virtual ICollection<StoryView> Views { get; set; } = new List<StoryView>();
}

public class StoryView : BaseEntity
{
    public Guid StoryId { get; set; }
    public Guid ViewerId { get; set; }

    public virtual Story Story { get; set; } = null!;
    public virtual AppUser Viewer { get; set; } = null!;
}

public class Hashtag : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int PostsCount { get; set; } = 0;

    public virtual ICollection<PostHashtag> PostHashtags { get; set; } = new List<PostHashtag>();
    public virtual ICollection<HashtagFollow> Followers { get; set; } = new List<HashtagFollow>();
}

public class PostHashtag
{
    public Guid PostId { get; set; }
    public Guid HashtagId { get; set; }

    public virtual Post Post { get; set; } = null!;
    public virtual Hashtag Hashtag { get; set; } = null!;
}

public class PostTag : BaseEntity
{
    public Guid PostId { get; set; }
    public Guid TaggedUserId { get; set; }
    public double? XPosition { get; set; }
    public double? YPosition { get; set; }

    public virtual Post Post { get; set; } = null!;
    public virtual AppUser TaggedUser { get; set; } = null!;
}

public class HashtagFollow : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid HashtagId { get; set; }

    public virtual AppUser User { get; set; } = null!;
    public virtual Hashtag Hashtag { get; set; } = null!;
}

public class SavedPost : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid PostId { get; set; }
    public Guid? CollectionId { get; set; }

    public virtual AppUser User { get; set; } = null!;
    public virtual Post Post { get; set; } = null!;
    public virtual SavedCollection? Collection { get; set; }
}

public class SavedCollection : BaseEntity
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }

    public virtual AppUser User { get; set; } = null!;
    public virtual ICollection<SavedPost> SavedPosts { get; set; } = new List<SavedPost>();
}

public class BlockedUser : BaseEntity
{
    public Guid BlockerId { get; set; }
    public Guid BlockedId { get; set; }

    public virtual AppUser Blocker { get; set; } = null!;
    public virtual AppUser Blocked { get; set; } = null!;
}
