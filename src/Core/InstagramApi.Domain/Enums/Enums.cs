namespace InstagramApi.Domain.Enums;

public enum PostType
{
    Image = 1,
    Video = 2,
    Reel = 3,
    Carousel = 4
}

public enum PostVisibility
{
    Public = 1,
    Followers = 2,
    Close = 3
}

public enum NotificationType
{
    Like = 1,
    Comment = 2,
    Follow = 3,
    FollowRequest = 4,
    FollowAccepted = 5,
    Mention = 6,
    Tag = 7,
    CommentLike = 8,
    CommentReply = 9,
    DirectMessage = 10,
    SystemAlert = 11
}

public enum ReportStatus
{
    Pending = 1,
    UnderReview = 2,
    Resolved = 3,
    Dismissed = 4
}

public enum UserRole
{
    User = 1,
    Moderator = 2,
    Admin = 3,
    SuperAdmin = 4
}
