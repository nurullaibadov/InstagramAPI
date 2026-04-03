using InstagramApi.Domain.Entities;

namespace InstagramApi.Application.Interfaces.Repositories;

public interface IUserRepository : IGenericRepository<AppUser>
{
    Task<AppUser?> GetByUsernameAsync(string username, CancellationToken ct = default);
    Task<AppUser?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<IEnumerable<AppUser>> SearchUsersAsync(string query, int page, int pageSize, CancellationToken ct = default);
    Task<IEnumerable<AppUser>> GetSuggestedUsersAsync(Guid userId, int count, CancellationToken ct = default);
    Task<bool> IsFollowingAsync(Guid followerId, Guid followingId, CancellationToken ct = default);
    Task<bool> IsBlockedAsync(Guid blockerId, Guid blockedId, CancellationToken ct = default);
    Task<IEnumerable<AppUser>> GetFollowersAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);
    Task<IEnumerable<AppUser>> GetFollowingAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);
}

public interface IPostRepository : IGenericRepository<Post>
{
    Task<Post?> GetPostWithDetailsAsync(Guid postId, CancellationToken ct = default);
    Task<IEnumerable<Post>> GetUserPostsAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);
    Task<IEnumerable<Post>> GetFeedPostsAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);
    Task<IEnumerable<Post>> GetExplorePostsAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);
    Task<IEnumerable<Post>> GetPostsByHashtagAsync(string hashtag, int page, int pageSize, CancellationToken ct = default);
    Task<bool> IsLikedByUserAsync(Guid postId, Guid userId, CancellationToken ct = default);
    Task<bool> IsSavedByUserAsync(Guid postId, Guid userId, CancellationToken ct = default);
}

public interface ICommentRepository : IGenericRepository<Comment>
{
    Task<IEnumerable<Comment>> GetPostCommentsAsync(Guid postId, int page, int pageSize, CancellationToken ct = default);
    Task<IEnumerable<Comment>> GetCommentRepliesAsync(Guid commentId, int page, int pageSize, CancellationToken ct = default);
    Task<bool> IsLikedByUserAsync(Guid commentId, Guid userId, CancellationToken ct = default);
}

public interface IStoryRepository : IGenericRepository<Story>
{
    Task<IEnumerable<Story>> GetActiveStoriesAsync(Guid userId, CancellationToken ct = default);
    Task<IEnumerable<Story>> GetFeedStoriesAsync(Guid userId, CancellationToken ct = default);
    Task<bool> IsViewedByUserAsync(Guid storyId, Guid userId, CancellationToken ct = default);
}

public interface IFollowRepository : IGenericRepository<Follow>
{
    Task<Follow?> GetFollowAsync(Guid followerId, Guid followingId, CancellationToken ct = default);
    Task<IEnumerable<Follow>> GetPendingRequestsAsync(Guid userId, CancellationToken ct = default);
}

public interface IMessageRepository : IGenericRepository<Message>
{
    Task<IEnumerable<Message>> GetConversationMessagesAsync(Guid conversationId, int page, int pageSize, CancellationToken ct = default);
    Task<IEnumerable<Conversation>> GetUserConversationsAsync(Guid userId, CancellationToken ct = default);
    Task<Conversation?> GetOrCreateConversationAsync(Guid user1Id, Guid user2Id, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default);
}

public interface INotificationRepository : IGenericRepository<Notification>
{
    Task<IEnumerable<Notification>> GetUserNotificationsAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default);
    Task MarkAllAsReadAsync(Guid userId, CancellationToken ct = default);
}

public interface IHashtagRepository : IGenericRepository<Hashtag>
{
    Task<Hashtag?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<IEnumerable<Hashtag>> SearchHashtagsAsync(string query, int count, CancellationToken ct = default);
    Task<IEnumerable<Hashtag>> GetTrendingHashtagsAsync(int count, CancellationToken ct = default);
}

public interface IReportRepository : IGenericRepository<Report>
{
    Task<IEnumerable<Report>> GetPendingReportsAsync(int page, int pageSize, CancellationToken ct = default);
}

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IPostRepository Posts { get; }
    ICommentRepository Comments { get; }
    IStoryRepository Stories { get; }
    IFollowRepository Follows { get; }
    IMessageRepository Messages { get; }
    INotificationRepository Notifications { get; }
    IHashtagRepository Hashtags { get; }
    IReportRepository Reports { get; }
    IGenericRepository<Like> Likes { get; }
    IGenericRepository<CommentLike> CommentLikes { get; }
    IGenericRepository<SavedPost> SavedPosts { get; }
    IGenericRepository<SavedCollection> SavedCollections { get; }
    IGenericRepository<BlockedUser> BlockedUsers { get; }
    IGenericRepository<AppSettings> AppSettings { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitTransactionAsync(CancellationToken ct = default);
    Task RollbackTransactionAsync(CancellationToken ct = default);
}
