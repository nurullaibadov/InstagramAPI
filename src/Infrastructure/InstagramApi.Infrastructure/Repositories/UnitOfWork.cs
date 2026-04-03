using InstagramApi.Application.Interfaces.Repositories;
using InstagramApi.Domain.Entities;
using InstagramApi.Persistence.Context;
using Microsoft.EntityFrameworkCore.Storage;

namespace InstagramApi.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IDbContextTransaction? _transaction;

    public IUserRepository Users { get; }
    public IPostRepository Posts { get; }
    public ICommentRepository Comments { get; }
    public IStoryRepository Stories { get; }
    public IFollowRepository Follows { get; }
    public IMessageRepository Messages { get; }
    public INotificationRepository Notifications { get; }
    public IHashtagRepository Hashtags { get; }
    public IReportRepository Reports { get; }
    public IGenericRepository<Like> Likes { get; }
    public IGenericRepository<CommentLike> CommentLikes { get; }
    public IGenericRepository<SavedPost> SavedPosts { get; }
    public IGenericRepository<SavedCollection> SavedCollections { get; }
    public IGenericRepository<BlockedUser> BlockedUsers { get; }
    public IGenericRepository<AppSettings> AppSettings { get; }

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
        Users = new UserRepository(context);
        Posts = new PostRepository(context);
        Comments = new CommentRepository(context);
        Stories = new StoryRepository(context);
        Follows = new FollowRepository(context);
        Messages = new MessageRepository(context);
        Notifications = new NotificationRepository(context);
        Hashtags = new HashtagRepository(context);
        Reports = new ReportRepository(context);
        Likes = new GenericRepository<Like>(context);
        CommentLikes = new GenericRepository<CommentLike>(context);
        SavedPosts = new GenericRepository<SavedPost>(context);
        SavedCollections = new GenericRepository<SavedCollection>(context);
        BlockedUsers = new GenericRepository<BlockedUser>(context);
        AppSettings = new GenericRepository<AppSettings>(context);
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);

    public async Task BeginTransactionAsync(CancellationToken ct = default)
        => _transaction = await _context.Database.BeginTransactionAsync(ct);

    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync(ct);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(ct);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
