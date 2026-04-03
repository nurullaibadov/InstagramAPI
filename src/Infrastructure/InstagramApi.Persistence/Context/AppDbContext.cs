using InstagramApi.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace InstagramApi.Persistence.Context;

public class AppDbContext : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Post> Posts => Set<Post>();
    public DbSet<PostMedia> PostMediaFiles => Set<PostMedia>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Like> Likes => Set<Like>();
    public DbSet<CommentLike> CommentLikes => Set<CommentLike>();
    public DbSet<Follow> Follows => Set<Follow>();
    public DbSet<Story> Stories => Set<Story>();
    public DbSet<StoryView> StoryViews => Set<StoryView>();
    public DbSet<Hashtag> Hashtags => Set<Hashtag>();
    public DbSet<PostHashtag> PostHashtags => Set<PostHashtag>();
    public DbSet<PostTag> PostTags => Set<PostTag>();
    public DbSet<HashtagFollow> HashtagFollows => Set<HashtagFollow>();
    public DbSet<SavedPost> SavedPosts => Set<SavedPost>();
    public DbSet<SavedCollection> SavedCollections => Set<SavedCollection>();
    public DbSet<BlockedUser> BlockedUsers => Set<BlockedUser>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationParticipant> ConversationParticipants => Set<ConversationParticipant>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<UserActivity> UserActivities => Set<UserActivity>();
    public DbSet<AppSettings> AppSettings => Set<AppSettings>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Rename Identity tables
        builder.Entity<AppUser>().ToTable("Users");
        builder.Entity<IdentityRole<Guid>>().ToTable("Roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");

        // Global soft delete filter
        builder.Entity<Post>().HasQueryFilter(p => !p.IsDeleted);
        builder.Entity<Comment>().HasQueryFilter(c => !c.IsDeleted);
        builder.Entity<Story>().HasQueryFilter(s => !s.IsDeleted);
        builder.Entity<Message>().HasQueryFilter(m => !m.IsDeleted);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is Domain.Common.BaseEntity entity)
            {
                if (entry.State == EntityState.Modified)
                    entity.UpdatedAt = DateTime.UtcNow;
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
