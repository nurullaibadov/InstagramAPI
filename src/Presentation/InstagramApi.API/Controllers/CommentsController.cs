using AutoMapper;
using InstagramApi.Application.DTOs.Comment;
using InstagramApi.Application.Interfaces.Repositories;
using InstagramApi.Application.Interfaces.Services;
using InstagramApi.Domain.Entities;
using InstagramApi.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InstagramApi.API.Controllers;

[Authorize]
public class CommentsController : BaseController
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly INotificationService _notificationService;

    public CommentsController(IUnitOfWork uow, IMapper mapper, INotificationService notificationService)
    {
        _uow = uow;
        _mapper = mapper;
        _notificationService = notificationService;
    }

    /// <summary>Get comments for a post</summary>
    [HttpGet("posts/{postId}/comments")]
    [AllowAnonymous]
    public async Task<IActionResult> GetComments(Guid postId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var post = await _uow.Posts.GetByIdAsync(postId);
        if (post == null) return ApiNotFound("Post not found");

        var comments = await _uow.Comments.GetPostCommentsAsync(postId, page, pageSize);
        var dtos = new List<CommentDto>();

        foreach (var c in comments)
        {
            var dto = _mapper.Map<CommentDto>(c);
            if (CurrentUser.IsAuthenticated)
                dto.IsLiked = await _uow.Comments.IsLikedByUserAsync(c.Id, CurrentUserId);
            dtos.Add(dto);
        }

        return ApiOk(dtos);
    }

    /// <summary>Get replies to a comment</summary>
    [HttpGet("{commentId}/replies")]
    [AllowAnonymous]
    public async Task<IActionResult> GetReplies(Guid commentId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var replies = await _uow.Comments.GetCommentRepliesAsync(commentId, page, pageSize);
        var dtos = new List<CommentDto>();

        foreach (var c in replies)
        {
            var dto = _mapper.Map<CommentDto>(c);
            if (CurrentUser.IsAuthenticated)
                dto.IsLiked = await _uow.Comments.IsLikedByUserAsync(c.Id, CurrentUserId);
            dtos.Add(dto);
        }

        return ApiOk(dtos);
    }

    /// <summary>Add a comment to a post</summary>
    [HttpPost("posts/{postId}/comments")]
    public async Task<IActionResult> AddComment(Guid postId, [FromBody] CreateCommentDto dto)
    {
        var post = await _uow.Posts.GetByIdAsync(postId);
        if (post == null) return ApiNotFound("Post not found");
        if (!post.CommentsEnabled) return ApiForbidden("Comments are disabled for this post");

        Comment? parentComment = null;
        if (dto.ParentCommentId.HasValue)
        {
            parentComment = await _uow.Comments.GetByIdAsync(dto.ParentCommentId.Value);
            if (parentComment == null) return ApiNotFound("Parent comment not found");
        }

        var comment = new Comment
        {
            PostId = postId,
            UserId = CurrentUserId,
            Text = dto.Text,
            ParentCommentId = dto.ParentCommentId
        };

        await _uow.Comments.AddAsync(comment);

        if (parentComment != null)
        {
            parentComment.RepliesCount++;
            await _uow.Comments.UpdateAsync(parentComment);
        }
        else
        {
            post.CommentsCount++;
            await _uow.Posts.UpdateAsync(post);
        }

        await _uow.SaveChangesAsync();

        // Send notification
        var notifTarget = dto.ParentCommentId.HasValue ? parentComment!.UserId : post.UserId;
        if (notifTarget != CurrentUserId)
        {
            var type = dto.ParentCommentId.HasValue ? NotificationType.CommentReply : NotificationType.Comment;
            var msg = dto.ParentCommentId.HasValue
                ? $"@{CurrentUser.Username} replied to your comment"
                : $"@{CurrentUser.Username} commented on your post";

            await _notificationService.SendNotificationAsync(notifTarget, CurrentUserId,
                type, msg, postId, "Post");
        }

        // Reload with user
        var saved = await _uow.Comments.GetByIdAsync(comment.Id);
        return ApiCreated(_mapper.Map<CommentDto>(saved!), "Comment added");
    }

    /// <summary>Delete a comment</summary>
    [HttpDelete("{commentId}")]
    public async Task<IActionResult> DeleteComment(Guid commentId)
    {
        var comment = await _uow.Comments.GetByIdAsync(commentId);
        if (comment == null) return ApiNotFound("Comment not found");

        var post = await _uow.Posts.GetByIdAsync(comment.PostId);

        var isOwner = comment.UserId == CurrentUserId;
        var isPostOwner = post?.UserId == CurrentUserId;
        var isAdmin = CurrentUser.IsInRole("Admin");

        if (!isOwner && !isPostOwner && !isAdmin)
            return ApiForbidden("Not authorized to delete this comment");

        await _uow.Comments.SoftDeleteAsync(comment);

        if (comment.ParentCommentId == null && post != null)
        {
            post.CommentsCount = Math.Max(0, post.CommentsCount - 1);
            await _uow.Posts.UpdateAsync(post);
        }

        await _uow.SaveChangesAsync();
        return ApiOk("Comment deleted");
    }

    /// <summary>Like a comment</summary>
    [HttpPost("{commentId}/like")]
    public async Task<IActionResult> LikeComment(Guid commentId)
    {
        var comment = await _uow.Comments.GetByIdAsync(commentId);
        if (comment == null) return ApiNotFound("Comment not found");

        var existing = await _uow.CommentLikes.GetAsync(l => l.CommentId == commentId && l.UserId == CurrentUserId);
        if (existing != null) return ApiBadRequest("Comment already liked");

        await _uow.CommentLikes.AddAsync(new CommentLike { CommentId = commentId, UserId = CurrentUserId });
        comment.LikesCount++;
        await _uow.Comments.UpdateAsync(comment);
        await _uow.SaveChangesAsync();

        if (comment.UserId != CurrentUserId)
            await _notificationService.SendNotificationAsync(comment.UserId, CurrentUserId,
                NotificationType.CommentLike,
                $"@{CurrentUser.Username} liked your comment", comment.PostId, "Post");

        return ApiOk(new { likesCount = comment.LikesCount });
    }

    /// <summary>Unlike a comment</summary>
    [HttpDelete("{commentId}/like")]
    public async Task<IActionResult> UnlikeComment(Guid commentId)
    {
        var like = await _uow.CommentLikes.GetAsync(l => l.CommentId == commentId && l.UserId == CurrentUserId);
        if (like == null) return ApiBadRequest("Comment not liked");

        var comment = await _uow.Comments.GetByIdAsync(commentId);
        if (comment != null)
        {
            comment.LikesCount = Math.Max(0, comment.LikesCount - 1);
            await _uow.Comments.UpdateAsync(comment);
        }

        await _uow.CommentLikes.DeleteAsync(like);
        await _uow.SaveChangesAsync();

        return ApiOk(new { likesCount = comment?.LikesCount ?? 0 });
    }
}
