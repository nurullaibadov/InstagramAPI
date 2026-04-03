using InstagramApi.Domain.Common;

namespace InstagramApi.Domain.Entities;

public class Comment : BaseEntity
{
    public Guid PostId { get; set; }
    public Guid UserId { get; set; }
    public Guid? ParentCommentId { get; set; }
    public string Text { get; set; } = string.Empty;
    public int LikesCount { get; set; } = 0;
    public int RepliesCount { get; set; } = 0;

    public virtual Post Post { get; set; } = null!;
    public virtual AppUser User { get; set; } = null!;
    public virtual Comment? ParentComment { get; set; }
    public virtual ICollection<Comment> Replies { get; set; } = new List<Comment>();
    public virtual ICollection<CommentLike> Likes { get; set; } = new List<CommentLike>();
}
