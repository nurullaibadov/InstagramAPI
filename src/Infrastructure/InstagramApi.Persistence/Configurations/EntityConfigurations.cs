using InstagramApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstagramApi.Persistence.Configurations;

public class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Caption).HasMaxLength(2200);
        builder.Property(p => p.Location).HasMaxLength(100);

        builder.HasOne(p => p.User)
            .WithMany(u => u.Posts)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.UserId);
        builder.HasIndex(p => p.CreatedAt);
    }
}

public class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Text).HasMaxLength(2200).IsRequired();

        builder.HasOne(c => c.User)
            .WithMany(u => u.Comments)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(c => c.Post)
            .WithMany(p => p.Comments)
            .HasForeignKey(c => c.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.ParentComment)
            .WithMany(c => c.Replies)
            .HasForeignKey(c => c.ParentCommentId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(c => c.PostId);
    }
}

public class LikeConfiguration : IEntityTypeConfiguration<Like>
{
    public void Configure(EntityTypeBuilder<Like> builder)
    {
        builder.HasKey(l => l.Id);
        builder.HasIndex(l => new { l.PostId, l.UserId }).IsUnique();

        builder.HasOne(l => l.Post)
            .WithMany(p => p.Likes)
            .HasForeignKey(l => l.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.User)
            .WithMany(u => u.Likes)
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public class FollowConfiguration : IEntityTypeConfiguration<Follow>
{
    public void Configure(EntityTypeBuilder<Follow> builder)
    {
        builder.HasKey(f => f.Id);
        builder.HasIndex(f => new { f.FollowerId, f.FollowingId }).IsUnique();

        builder.HasOne(f => f.Follower)
            .WithMany(u => u.Following)
            .HasForeignKey(f => f.FollowerId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(f => f.Following)
            .WithMany(u => u.Followers)
            .HasForeignKey(f => f.FollowingId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public class PostHashtagConfiguration : IEntityTypeConfiguration<PostHashtag>
{
    public void Configure(EntityTypeBuilder<PostHashtag> builder)
    {
        builder.HasKey(ph => new { ph.PostId, ph.HashtagId });

        builder.HasOne(ph => ph.Post)
            .WithMany(p => p.PostHashtags)
            .HasForeignKey(ph => ph.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ph => ph.Hashtag)
            .WithMany(h => h.PostHashtags)
            .HasForeignKey(ph => ph.HashtagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class BlockedUserConfiguration : IEntityTypeConfiguration<BlockedUser>
{
    public void Configure(EntityTypeBuilder<BlockedUser> builder)
    {
        builder.HasKey(b => b.Id);
        builder.HasIndex(b => new { b.BlockerId, b.BlockedId }).IsUnique();

        builder.HasOne(b => b.Blocker)
            .WithMany(u => u.BlockedUsers)
            .HasForeignKey(b => b.BlockerId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(b => b.Blocked)
            .WithMany(u => u.BlockedByUsers)
            .HasForeignKey(b => b.BlockedId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.HasKey(m => m.Id);

        builder.HasOne(m => m.Sender)
            .WithMany(u => u.SentMessages)
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(m => m.Receiver)
            .WithMany(u => u.ReceivedMessages)
            .HasForeignKey(m => m.ReceiverId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(m => m.ConversationId);
        builder.HasIndex(m => m.CreatedAt);
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Message).HasMaxLength(500);

        builder.HasOne(n => n.User)
            .WithMany(u => u.Notifications)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(n => n.Actor)
            .WithMany()
            .HasForeignKey(n => n.ActorId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(n => n.UserId);
        builder.HasIndex(n => n.IsRead);
    }
}

public class HashtagConfiguration : IEntityTypeConfiguration<Hashtag>
{
    public void Configure(EntityTypeBuilder<Hashtag> builder)
    {
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(h => h.Name).IsUnique();
    }
}

public class StoryConfiguration : IEntityTypeConfiguration<Story>
{
    public void Configure(EntityTypeBuilder<Story> builder)
    {
        builder.HasKey(s => s.Id);

        builder.HasOne(s => s.User)
            .WithMany(u => u.Stories)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => s.UserId);
        builder.HasIndex(s => s.ExpiresAt);
    }
}
