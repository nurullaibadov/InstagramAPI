using AutoMapper;
using InstagramApi.Application.DTOs.Post;
using InstagramApi.Application.Interfaces.Repositories;
using InstagramApi.Application.Interfaces.Services;
using InstagramApi.Domain.Entities;
using InstagramApi.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InstagramApi.API.Controllers;

[Authorize]
public class PostsController : BaseController
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly IFileService _fileService;
    private readonly INotificationService _notificationService;

    public PostsController(IUnitOfWork uow, IMapper mapper,
        IFileService fileService, INotificationService notificationService)
    {
        _uow = uow;
        _mapper = mapper;
        _fileService = fileService;
        _notificationService = notificationService;
    }

    /// <summary>Get home feed posts</summary>
    [HttpGet("feed")]
    public async Task<IActionResult> GetFeed([FromQuery] int page = 1, [FromQuery] int pageSize = 12)
    {
        var posts = await _uow.Posts.GetFeedPostsAsync(CurrentUserId, page, pageSize);
        var dtos = new List<PostDto>();

        foreach (var post in posts)
        {
            var dto = _mapper.Map<PostDto>(post);
            dto.IsLiked = await _uow.Posts.IsLikedByUserAsync(post.Id, CurrentUserId);
            dto.IsSaved = await _uow.Posts.IsSavedByUserAsync(post.Id, CurrentUserId);
            dtos.Add(dto);
        }

        return ApiOk(dtos);
    }

    /// <summary>Get explore/discover posts</summary>
    [HttpGet("explore")]
    public async Task<IActionResult> GetExplore([FromQuery] int page = 1, [FromQuery] int pageSize = 24)
    {
        var posts = await _uow.Posts.GetExplorePostsAsync(CurrentUserId, page, pageSize);
        var dtos = new List<PostDto>();

        foreach (var post in posts)
        {
            var dto = _mapper.Map<PostDto>(post);
            dto.IsLiked = await _uow.Posts.IsLikedByUserAsync(post.Id, CurrentUserId);
            dto.IsSaved = await _uow.Posts.IsSavedByUserAsync(post.Id, CurrentUserId);
            dtos.Add(dto);
        }

        return ApiOk(dtos);
    }

    /// <summary>Get a single post by ID</summary>
    [HttpGet("{postId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPost(Guid postId)
    {
        var post = await _uow.Posts.GetPostWithDetailsAsync(postId);
        if (post == null) return ApiNotFound("Post not found");

        // Increment view count
        post.ViewsCount++;
        await _uow.Posts.UpdateAsync(post);
        await _uow.SaveChangesAsync();

        var dto = _mapper.Map<PostDto>(post);

        if (CurrentUser.IsAuthenticated)
        {
            dto.IsLiked = await _uow.Posts.IsLikedByUserAsync(postId, CurrentUserId);
            dto.IsSaved = await _uow.Posts.IsSavedByUserAsync(postId, CurrentUserId);
        }

        return ApiOk(dto);
    }

    /// <summary>Get posts by user</summary>
    [HttpGet("user/{userId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetUserPosts(Guid userId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 12)
    {
        var posts = await _uow.Posts.GetUserPostsAsync(userId, page, pageSize);
        var dtos = new List<PostDto>();

        foreach (var post in posts)
        {
            var dto = _mapper.Map<PostDto>(post);
            if (CurrentUser.IsAuthenticated)
            {
                dto.IsLiked = await _uow.Posts.IsLikedByUserAsync(post.Id, CurrentUserId);
                dto.IsSaved = await _uow.Posts.IsSavedByUserAsync(post.Id, CurrentUserId);
            }
            dtos.Add(dto);
        }

        return ApiOk(dtos);
    }

    /// <summary>Get posts by hashtag</summary>
    [HttpGet("hashtag/{hashtag}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByHashtag(string hashtag,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 24)
    {
        var posts = await _uow.Posts.GetPostsByHashtagAsync(hashtag, page, pageSize);
        var dtos = new List<PostDto>();

        foreach (var post in posts)
        {
            var dto = _mapper.Map<PostDto>(post);
            if (CurrentUser.IsAuthenticated)
                dto.IsLiked = await _uow.Posts.IsLikedByUserAsync(post.Id, CurrentUserId);
            dtos.Add(dto);
        }

        return ApiOk(dtos);
    }

    /// <summary>Create a new post</summary>
    [HttpPost]
    [RequestSizeLimit(500_000_000)]
    public async Task<IActionResult> CreatePost([FromForm] CreatePostDto dto)
    {
        if (dto.MediaFiles == null || !dto.MediaFiles.Any())
            return ApiBadRequest("At least one media file is required");

        foreach (var file in dto.MediaFiles)
        {
            if (!_fileService.IsValidImageFile(file) && !_fileService.IsValidVideoFile(file))
                return ApiBadRequest($"Invalid file: {file.FileName}");
        }

        var post = new Post
        {
            UserId = CurrentUserId,
            Caption = dto.Caption,
            Location = dto.Location,
            PostType = dto.PostType,
            CommentsEnabled = dto.CommentsEnabled,
            LikesVisible = dto.LikesVisible,
            Visibility = dto.Visibility
        };

        await _uow.Posts.AddAsync(post);

        // Upload media files
        int order = 0;
        foreach (var file in dto.MediaFiles)
        {
            string url;
            string mediaType;

            if (_fileService.IsValidVideoFile(file))
            {
                url = await _fileService.UploadVideoAsync(file, $"posts/{post.Id}/videos");
                mediaType = "video";
            }
            else
            {
                url = await _fileService.ProcessAndUploadImageAsync(file, $"posts/{post.Id}/images", 1080);
                mediaType = "image";
            }

            post.MediaFiles.Add(new PostMedia
            {
                PostId = post.Id,
                Url = url,
                MediaType = mediaType,
                Order = order++
            });
        }

        // Handle hashtags
        if (dto.Hashtags?.Any() == true)
        {
            foreach (var tag in dto.Hashtags.Distinct())
            {
                var normalizedTag = tag.ToLower().TrimStart('#');
                var hashtag = await _uow.Hashtags.GetByNameAsync(normalizedTag)
                    ?? new Hashtag { Name = normalizedTag };

                if (hashtag.Id == Guid.Empty)
                    await _uow.Hashtags.AddAsync(hashtag);

                hashtag.PostsCount++;
                post.PostHashtags.Add(new PostHashtag { PostId = post.Id, HashtagId = hashtag.Id });
            }
        }

        // Handle tagged users
        if (dto.TaggedUserIds?.Any() == true)
        {
            foreach (var taggedUserId in dto.TaggedUserIds.Distinct())
            {
                post.Tags.Add(new PostTag { PostId = post.Id, TaggedUserId = taggedUserId });

                await _notificationService.SendNotificationAsync(taggedUserId, CurrentUserId,
                    NotificationType.Tag,
                    $"@{CurrentUser.Username} tagged you in a post", post.Id, "Post");
            }
        }

        // Update user post count
        var user = await _uow.Users.GetByIdAsync(CurrentUserId);
        if (user != null)
        {
            user.PostsCount++;
            await _uow.Users.UpdateAsync(user);
        }

        await _uow.SaveChangesAsync();

        var result = await _uow.Posts.GetPostWithDetailsAsync(post.Id);
        return ApiCreated(_mapper.Map<PostDto>(result!), "Post created");
    }

    /// <summary>Update a post</summary>
    [HttpPut("{postId}")]
    public async Task<IActionResult> UpdatePost(Guid postId, [FromBody] UpdatePostDto dto)
    {
        var post = await _uow.Posts.GetByIdAsync(postId);
        if (post == null) return ApiNotFound("Post not found");
        if (post.UserId != CurrentUserId) return ApiForbidden("You can only edit your own posts");

        if (dto.Caption != null) post.Caption = dto.Caption;
        if (dto.Location != null) post.Location = dto.Location;
        if (dto.CommentsEnabled.HasValue) post.CommentsEnabled = dto.CommentsEnabled.Value;
        if (dto.LikesVisible.HasValue) post.LikesVisible = dto.LikesVisible.Value;

        await _uow.Posts.UpdateAsync(post);
        await _uow.SaveChangesAsync();

        return ApiOk(_mapper.Map<PostDto>(post), "Post updated");
    }

    /// <summary>Delete a post</summary>
    [HttpDelete("{postId}")]
    public async Task<IActionResult> DeletePost(Guid postId)
    {
        var post = await _uow.Posts.GetPostWithDetailsAsync(postId);
        if (post == null) return ApiNotFound("Post not found");
        if (post.UserId != CurrentUserId && !CurrentUser.IsInRole("Admin"))
            return ApiForbidden("You can only delete your own posts");

        // Delete media files
        foreach (var media in post.MediaFiles)
            await _fileService.DeleteFileAsync(media.Url);

        await _uow.Posts.SoftDeleteAsync(post);

        var user = await _uow.Users.GetByIdAsync(post.UserId);
        if (user != null)
        {
            user.PostsCount = Math.Max(0, user.PostsCount - 1);
            await _uow.Users.UpdateAsync(user);
        }

        await _uow.SaveChangesAsync();
        return ApiOk("Post deleted");
    }

    /// <summary>Like a post</summary>
    [HttpPost("{postId}/like")]
    public async Task<IActionResult> LikePost(Guid postId)
    {
        var post = await _uow.Posts.GetByIdAsync(postId);
        if (post == null) return ApiNotFound("Post not found");

        var existing = await _uow.Likes.GetAsync(l => l.PostId == postId && l.UserId == CurrentUserId);
        if (existing != null) return ApiBadRequest("Post already liked");

        await _uow.Likes.AddAsync(new Like { PostId = postId, UserId = CurrentUserId });
        post.LikesCount++;
        await _uow.Posts.UpdateAsync(post);
        await _uow.SaveChangesAsync();

        if (post.UserId != CurrentUserId)
            await _notificationService.SendNotificationAsync(post.UserId, CurrentUserId,
                NotificationType.Like,
                $"@{CurrentUser.Username} liked your post", postId, "Post");

        return ApiOk(new { likesCount = post.LikesCount });
    }

    /// <summary>Unlike a post</summary>
    [HttpDelete("{postId}/like")]
    public async Task<IActionResult> UnlikePost(Guid postId)
    {
        var like = await _uow.Likes.GetAsync(l => l.PostId == postId && l.UserId == CurrentUserId);
        if (like == null) return ApiBadRequest("Post not liked");

        var post = await _uow.Posts.GetByIdAsync(postId);
        if (post != null)
        {
            post.LikesCount = Math.Max(0, post.LikesCount - 1);
            await _uow.Posts.UpdateAsync(post);
        }

        await _uow.Likes.DeleteAsync(like);
        await _uow.SaveChangesAsync();

        return ApiOk(new { likesCount = post?.LikesCount ?? 0 });
    }

    /// <summary>Save a post</summary>
    [HttpPost("{postId}/save")]
    public async Task<IActionResult> SavePost(Guid postId, [FromQuery] Guid? collectionId = null)
    {
        var existing = await _uow.SavedPosts.GetAsync(s => s.PostId == postId && s.UserId == CurrentUserId);
        if (existing != null) return ApiBadRequest("Post already saved");

        await _uow.SavedPosts.AddAsync(new SavedPost
        {
            UserId = CurrentUserId,
            PostId = postId,
            CollectionId = collectionId
        });
        await _uow.SaveChangesAsync();

        return ApiOk("Post saved");
    }

    /// <summary>Unsave a post</summary>
    [HttpDelete("{postId}/save")]
    public async Task<IActionResult> UnsavePost(Guid postId)
    {
        var saved = await _uow.SavedPosts.GetAsync(s => s.PostId == postId && s.UserId == CurrentUserId);
        if (saved == null) return ApiBadRequest("Post not saved");

        await _uow.SavedPosts.DeleteAsync(saved);
        await _uow.SaveChangesAsync();

        return ApiOk("Post unsaved");
    }

    /// <summary>Get saved posts</summary>
    [HttpGet("saved")]
    public async Task<IActionResult> GetSavedPosts([FromQuery] int page = 1, [FromQuery] int pageSize = 12)
    {
        var saved = await _uow.SavedPosts.GetAllAsync(s => s.UserId == CurrentUserId);
        var postIds = saved.OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => s.PostId);

        var posts = new List<PostDto>();
        foreach (var id in postIds)
        {
            var post = await _uow.Posts.GetPostWithDetailsAsync(id);
            if (post != null)
            {
                var dto = _mapper.Map<PostDto>(post);
                dto.IsSaved = true;
                dto.IsLiked = await _uow.Posts.IsLikedByUserAsync(id, CurrentUserId);
                posts.Add(dto);
            }
        }

        return ApiOk(posts);
    }

    /// <summary>Report a post</summary>
    [HttpPost("{postId}/report")]
    public async Task<IActionResult> ReportPost(Guid postId, [FromBody] Application.DTOs.Admin.CreateReportDto dto)
    {
        var post = await _uow.Posts.GetByIdAsync(postId);
        if (post == null) return ApiNotFound("Post not found");

        var report = new Report
        {
            ReporterId = CurrentUserId,
            PostId = postId,
            Reason = dto.Reason,
            Description = dto.Description
        };

        await _uow.Reports.AddAsync(report);
        await _uow.SaveChangesAsync();

        return ApiCreated(null!, "Report submitted");
    }

    /// <summary>Get users who liked a post</summary>
    [HttpGet("{postId}/likes")]
    public async Task<IActionResult> GetLikes(Guid postId, [FromQuery] int page = 1, [FromQuery] int pageSize = 30)
    {
        var likes = (await _uow.Likes.GetAllAsync(l => l.PostId == postId))
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize);

        var users = new List<Application.DTOs.User.UserSummaryDto>();
        foreach (var like in likes)
        {
            var u = await _uow.Users.GetByIdAsync(like.UserId);
            if (u != null)
            {
                var dto = _mapper.Map<Application.DTOs.User.UserSummaryDto>(u);
                dto.IsFollowing = await _uow.Users.IsFollowingAsync(CurrentUserId, u.Id);
                users.Add(dto);
            }
        }

        return ApiOk(users);
    }
}
