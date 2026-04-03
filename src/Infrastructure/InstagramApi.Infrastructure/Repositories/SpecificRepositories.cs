using InstagramApi.Application.Interfaces.Repositories;
using InstagramApi.Domain.Entities;
using InstagramApi.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace InstagramApi.Infrastructure.Repositories;

public class UserRepository : GenericRepository<AppUser>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context) { }

    public async Task<AppUser?> GetByUsernameAsync(string username, CancellationToken ct = default)
        => await _context.Users.FirstOrDefaultAsync(u => u.UserName == username && !u.IsDeleted, ct);

    public async Task<AppUser?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await _context.Users.FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted, ct);

    public async Task<IEnumerable<AppUser>> SearchUsersAsync(string query, int page, int pageSize, CancellationToken ct = default)
        => await _context.Users
            .Where(u => !u.IsDeleted && u.IsActive &&
                (u.UserName!.Contains(query) || u.FullName.Contains(query)))
            .OrderBy(u => u.UserName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    public async Task<IEnumerable<AppUser>> GetSuggestedUsersAsync(Guid userId, int count, CancellationToken ct = default)
    {
        var following = await _context.Follows
            .Where(f => f.FollowerId == userId)
            .Select(f => f.FollowingId)
            .ToListAsync(ct);

        following.Add(userId);

        return await _context.Users
            .Where(u => !following.Contains(u.Id) && !u.IsDeleted && u.IsActive)
            .OrderByDescending(u => u.FollowersCount)
            .Take(count)
            .ToListAsync(ct);
    }

    public async Task<bool> IsFollowingAsync(Guid followerId, Guid followingId, CancellationToken ct = default)
        => await _context.Follows.AnyAsync(f => f.FollowerId == followerId && f.FollowingId == followingId && f.IsAccepted, ct);

    public async Task<bool> IsBlockedAsync(Guid blockerId, Guid blockedId, CancellationToken ct = default)
        => await _context.BlockedUsers.AnyAsync(b => b.BlockerId == blockerId && b.BlockedId == blockedId, ct);

    public async Task<IEnumerable<AppUser>> GetFollowersAsync(Guid userId, int page, int pageSize, CancellationToken ct = default)
        => await _context.Follows
            .Where(f => f.FollowingId == userId && f.IsAccepted)
            .Include(f => f.Follower)
            .Select(f => f.Follower)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    public async Task<IEnumerable<AppUser>> GetFollowingAsync(Guid userId, int page, int pageSize, CancellationToken ct = default)
        => await _context.Follows
            .Where(f => f.FollowerId == userId && f.IsAccepted)
            .Include(f => f.Following)
            .Select(f => f.Following)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
}

public class PostRepository : GenericRepository<Post>, IPostRepository
{
    public PostRepository(AppDbContext context) : base(context) { }

    public async Task<Post?> GetPostWithDetailsAsync(Guid postId, CancellationToken ct = default)
        => await _context.Posts
            .Include(p => p.User)
            .Include(p => p.MediaFiles)
            .Include(p => p.PostHashtags).ThenInclude(ph => ph.Hashtag)
            .Include(p => p.Tags).ThenInclude(t => t.TaggedUser)
            .FirstOrDefaultAsync(p => p.Id == postId, ct);

    public async Task<IEnumerable<Post>> GetUserPostsAsync(Guid userId, int page, int pageSize, CancellationToken ct = default)
        => await _context.Posts
            .Where(p => p.UserId == userId)
            .Include(p => p.MediaFiles)
            .Include(p => p.User)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    public async Task<IEnumerable<Post>> GetFeedPostsAsync(Guid userId, int page, int pageSize, CancellationToken ct = default)
    {
        var followingIds = await _context.Follows
            .Where(f => f.FollowerId == userId && f.IsAccepted)
            .Select(f => f.FollowingId)
            .ToListAsync(ct);

        followingIds.Add(userId);

        return await _context.Posts
            .Where(p => followingIds.Contains(p.UserId))
            .Include(p => p.User)
            .Include(p => p.MediaFiles)
            .Include(p => p.PostHashtags).ThenInclude(ph => ph.Hashtag)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<Post>> GetExplorePostsAsync(Guid userId, int page, int pageSize, CancellationToken ct = default)
        => await _context.Posts
            .Where(p => p.Visibility == Domain.Enums.PostVisibility.Public && p.UserId != userId)
            .Include(p => p.User)
            .Include(p => p.MediaFiles)
            .OrderByDescending(p => p.LikesCount)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    public async Task<IEnumerable<Post>> GetPostsByHashtagAsync(string hashtag, int page, int pageSize, CancellationToken ct = default)
        => await _context.Posts
            .Where(p => p.PostHashtags.Any(ph => ph.Hashtag.Name == hashtag.ToLower()))
            .Include(p => p.User)
            .Include(p => p.MediaFiles)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    public async Task<bool> IsLikedByUserAsync(Guid postId, Guid userId, CancellationToken ct = default)
        => await _context.Likes.AnyAsync(l => l.PostId == postId && l.UserId == userId, ct);

    public async Task<bool> IsSavedByUserAsync(Guid postId, Guid userId, CancellationToken ct = default)
        => await _context.SavedPosts.AnyAsync(s => s.PostId == postId && s.UserId == userId, ct);
}

public class CommentRepository : GenericRepository<Comment>, ICommentRepository
{
    public CommentRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Comment>> GetPostCommentsAsync(Guid postId, int page, int pageSize, CancellationToken ct = default)
        => await _context.Comments
            .Where(c => c.PostId == postId && c.ParentCommentId == null)
            .Include(c => c.User)
            .OrderByDescending(c => c.LikesCount)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    public async Task<IEnumerable<Comment>> GetCommentRepliesAsync(Guid commentId, int page, int pageSize, CancellationToken ct = default)
        => await _context.Comments
            .Where(c => c.ParentCommentId == commentId)
            .Include(c => c.User)
            .OrderBy(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    public async Task<bool> IsLikedByUserAsync(Guid commentId, Guid userId, CancellationToken ct = default)
        => await _context.CommentLikes.AnyAsync(l => l.CommentId == commentId && l.UserId == userId, ct);
}

public class StoryRepository : GenericRepository<Story>, IStoryRepository
{
    public StoryRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Story>> GetActiveStoriesAsync(Guid userId, CancellationToken ct = default)
        => await _context.Stories
            .Where(s => s.UserId == userId && s.ExpiresAt > DateTime.UtcNow)
            .OrderBy(s => s.CreatedAt)
            .ToListAsync(ct);

    public async Task<IEnumerable<Story>> GetFeedStoriesAsync(Guid userId, CancellationToken ct = default)
    {
        var followingIds = await _context.Follows
            .Where(f => f.FollowerId == userId && f.IsAccepted)
            .Select(f => f.FollowingId)
            .ToListAsync(ct);

        followingIds.Add(userId);

        return await _context.Stories
            .Where(s => followingIds.Contains(s.UserId) && s.ExpiresAt > DateTime.UtcNow)
            .Include(s => s.User)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<bool> IsViewedByUserAsync(Guid storyId, Guid userId, CancellationToken ct = default)
        => await _context.StoryViews.AnyAsync(v => v.StoryId == storyId && v.ViewerId == userId, ct);
}

public class FollowRepository : GenericRepository<Follow>, IFollowRepository
{
    public FollowRepository(AppDbContext context) : base(context) { }

    public async Task<Follow?> GetFollowAsync(Guid followerId, Guid followingId, CancellationToken ct = default)
        => await _context.Follows.FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowingId == followingId, ct);

    public async Task<IEnumerable<Follow>> GetPendingRequestsAsync(Guid userId, CancellationToken ct = default)
        => await _context.Follows
            .Where(f => f.FollowingId == userId && !f.IsAccepted)
            .Include(f => f.Follower)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(ct);
}

public class MessageRepository : GenericRepository<Message>, IMessageRepository
{
    public MessageRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Message>> GetConversationMessagesAsync(Guid conversationId, int page, int pageSize, CancellationToken ct = default)
        => await _context.Messages
            .Where(m => m.ConversationId == conversationId && !m.IsUnsent)
            .Include(m => m.Sender)
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    public async Task<IEnumerable<Conversation>> GetUserConversationsAsync(Guid userId, CancellationToken ct = default)
        => await _context.Conversations
            .Where(c => c.Participants.Any(p => p.UserId == userId))
            .Include(c => c.Participants).ThenInclude(p => p.User)
            .Include(c => c.Messages.OrderByDescending(m => m.CreatedAt).Take(1))
            .OrderByDescending(c => c.LastMessageAt)
            .ToListAsync(ct);

    public async Task<Conversation?> GetOrCreateConversationAsync(Guid user1Id, Guid user2Id, CancellationToken ct = default)
    {
        var existing = await _context.Conversations
            .Where(c => !c.IsGroup &&
                c.Participants.Any(p => p.UserId == user1Id) &&
                c.Participants.Any(p => p.UserId == user2Id))
            .Include(c => c.Participants).ThenInclude(p => p.User)
            .FirstOrDefaultAsync(ct);

        if (existing != null) return existing;

        var conversation = new Conversation
        {
            Participants = new List<ConversationParticipant>
            {
                new() { UserId = user1Id },
                new() { UserId = user2Id }
            }
        };
        await _context.Conversations.AddAsync(conversation, ct);
        return conversation;
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default)
        => await _context.Messages
            .CountAsync(m => m.ReceiverId == userId && !m.IsRead && !m.IsUnsent, ct);
}

public class NotificationRepository : GenericRepository<Notification>, INotificationRepository
{
    public NotificationRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(Guid userId, int page, int pageSize, CancellationToken ct = default)
        => await _context.Notifications
            .Where(n => n.UserId == userId)
            .Include(n => n.Actor)
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default)
        => await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead, ct);

    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken ct = default)
    {
        await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAt, DateTime.UtcNow), ct);
    }
}

public class HashtagRepository : GenericRepository<Hashtag>, IHashtagRepository
{
    public HashtagRepository(AppDbContext context) : base(context) { }

    public async Task<Hashtag?> GetByNameAsync(string name, CancellationToken ct = default)
        => await _context.Hashtags.FirstOrDefaultAsync(h => h.Name == name.ToLower(), ct);

    public async Task<IEnumerable<Hashtag>> SearchHashtagsAsync(string query, int count, CancellationToken ct = default)
        => await _context.Hashtags
            .Where(h => h.Name.Contains(query.ToLower()))
            .OrderByDescending(h => h.PostsCount)
            .Take(count)
            .ToListAsync(ct);

    public async Task<IEnumerable<Hashtag>> GetTrendingHashtagsAsync(int count, CancellationToken ct = default)
        => await _context.Hashtags
            .OrderByDescending(h => h.PostsCount)
            .Take(count)
            .ToListAsync(ct);
}

public class ReportRepository : GenericRepository<Report>, IReportRepository
{
    public ReportRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Report>> GetPendingReportsAsync(int page, int pageSize, CancellationToken ct = default)
        => await _context.Reports
            .Where(r => r.Status == Domain.Enums.ReportStatus.Pending)
            .Include(r => r.Reporter)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
}
