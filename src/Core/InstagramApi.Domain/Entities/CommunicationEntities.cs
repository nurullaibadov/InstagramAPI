using InstagramApi.Domain.Common;
using InstagramApi.Domain.Enums;

namespace InstagramApi.Domain.Entities;

public class Conversation : BaseEntity
{
    public bool IsGroup { get; set; } = false;
    public string? GroupName { get; set; }
    public string? GroupImageUrl { get; set; }
    public Guid? AdminId { get; set; }
    public DateTime? LastMessageAt { get; set; }

    public virtual ICollection<ConversationParticipant> Participants { get; set; } = new List<ConversationParticipant>();
    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
}

public class ConversationParticipant : BaseEntity
{
    public Guid ConversationId { get; set; }
    public Guid UserId { get; set; }
    public DateTime? LastReadAt { get; set; }
    public bool IsMuted { get; set; } = false;

    public virtual Conversation Conversation { get; set; } = null!;
    public virtual AppUser User { get; set; } = null!;
}

public class Message : BaseEntity
{
    public Guid ConversationId { get; set; }
    public Guid SenderId { get; set; }
    public Guid ReceiverId { get; set; }
    public string? Text { get; set; }
    public string? MediaUrl { get; set; }
    public string? MediaType { get; set; }
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
    public Guid? ReplyToMessageId { get; set; }
    public bool IsUnsent { get; set; } = false;

    public virtual Conversation Conversation { get; set; } = null!;
    public virtual AppUser Sender { get; set; } = null!;
    public virtual AppUser Receiver { get; set; } = null!;
    public virtual Message? ReplyToMessage { get; set; }
}

public class Notification : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid? ActorId { get; set; }
    public NotificationType Type { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? ReferenceId { get; set; }
    public string? ReferenceType { get; set; }
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }

    public virtual AppUser User { get; set; } = null!;
    public virtual AppUser? Actor { get; set; }
}

public class Report : BaseEntity
{
    public Guid ReporterId { get; set; }
    public Guid? PostId { get; set; }
    public Guid? UserId { get; set; }
    public Guid? CommentId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ReportStatus Status { get; set; } = ReportStatus.Pending;
    public Guid? ReviewedByAdminId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? AdminNote { get; set; }

    public virtual AppUser Reporter { get; set; } = null!;
    public virtual Post? Post { get; set; }
    public virtual AppUser? ReportedUser { get; set; }
}

public class UserActivity : BaseEntity
{
    public Guid UserId { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Details { get; set; }

    public virtual AppUser User { get; set; } = null!;
}

public class AppSettings : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Group { get; set; } = "General";
}
