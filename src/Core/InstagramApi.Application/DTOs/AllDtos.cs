using InstagramApi.Application.DTOs.User;
using InstagramApi.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace InstagramApi.Application.DTOs.Comment;

public class CommentDto
{
    public Guid Id { get; set; }
    public UserSummaryDto User { get; set; } = null!;
    public string Text { get; set; } = string.Empty;
    public int LikesCount { get; set; }
    public int RepliesCount { get; set; }
    public bool IsLiked { get; set; }
    public Guid? ParentCommentId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateCommentDto
{
    public string Text { get; set; } = string.Empty;
    public Guid? ParentCommentId { get; set; }
}

namespace InstagramApi.Application.DTOs.Story;

public class StoryDto
{
    public Guid Id { get; set; }
    public UserSummaryDto User { get; set; } = null!;
    public string MediaUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string MediaType { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public string? Location { get; set; }
    public int ViewsCount { get; set; }
    public bool IsViewed { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class StoryFeedDto
{
    public UserSummaryDto User { get; set; } = null!;
    public List<StoryDto> Stories { get; set; } = new();
    public bool HasUnviewed { get; set; }
}

public class CreateStoryDto
{
    public IFormFile MediaFile { get; set; } = null!;
    public string? Caption { get; set; }
    public string? Location { get; set; }
}

namespace InstagramApi.Application.DTOs.Notification;

public class NotificationDto
{
    public Guid Id { get; set; }
    public UserSummaryDto? Actor { get; set; }
    public NotificationType Type { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? ReferenceId { get; set; }
    public string? ReferenceType { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

namespace InstagramApi.Application.DTOs.Message;

public class MessageDto
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public UserSummaryDto Sender { get; set; } = null!;
    public string? Text { get; set; }
    public string? MediaUrl { get; set; }
    public string? MediaType { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public Guid? ReplyToMessageId { get; set; }
    public MessageDto? ReplyToMessage { get; set; }
    public bool IsUnsent { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ConversationDto
{
    public Guid Id { get; set; }
    public bool IsGroup { get; set; }
    public string? GroupName { get; set; }
    public string? GroupImageUrl { get; set; }
    public List<UserSummaryDto> Participants { get; set; } = new();
    public MessageDto? LastMessage { get; set; }
    public int UnreadCount { get; set; }
    public DateTime? LastMessageAt { get; set; }
}

public class SendMessageDto
{
    public Guid ReceiverId { get; set; }
    public string? Text { get; set; }
    public IFormFile? MediaFile { get; set; }
    public Guid? ReplyToMessageId { get; set; }
}

namespace InstagramApi.Application.DTOs.Admin;

public class AdminUserDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public bool IsActive { get; set; }
    public bool IsVerified { get; set; }
    public bool EmailConfirmed { get; set; }
    public int PostsCount { get; set; }
    public int FollowersCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> Roles { get; set; } = new();
}

public class AdminDashboardDto
{
    public int TotalUsers { get; set; }
    public int NewUsersToday { get; set; }
    public int NewUsersThisWeek { get; set; }
    public int TotalPosts { get; set; }
    public int NewPostsToday { get; set; }
    public int TotalReports { get; set; }
    public int PendingReports { get; set; }
    public int ActiveUsers { get; set; }
    public List<DailyStatsDto> DailyStats { get; set; } = new();
}

public class DailyStatsDto
{
    public DateTime Date { get; set; }
    public int NewUsers { get; set; }
    public int NewPosts { get; set; }
    public int NewReports { get; set; }
}

public class AdminUpdateUserDto
{
    public bool? IsActive { get; set; }
    public bool? IsVerified { get; set; }
    public string? Role { get; set; }
}

public class ReportDto
{
    public Guid Id { get; set; }
    public UserSummaryDto Reporter { get; set; } = null!;
    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ReportStatus Status { get; set; }
    public Guid? PostId { get; set; }
    public Guid? UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? AdminNote { get; set; }
}

public class ResolveReportDto
{
    public ReportStatus Status { get; set; }
    public string? AdminNote { get; set; }
}

public class CreateReportDto
{
    public Guid? PostId { get; set; }
    public Guid? UserId { get; set; }
    public Guid? CommentId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }
}
