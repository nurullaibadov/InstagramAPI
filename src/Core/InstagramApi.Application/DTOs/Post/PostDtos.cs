using InstagramApi.Application.DTOs.User;
using InstagramApi.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace InstagramApi.Application.DTOs.Post;

public class PostDto
{
    public Guid Id { get; set; }
    public UserSummaryDto User { get; set; } = null!;
    public string? Caption { get; set; }
    public string? Location { get; set; }
    public PostType PostType { get; set; }
    public int LikesCount { get; set; }
    public int CommentsCount { get; set; }
    public int ViewsCount { get; set; }
    public bool CommentsEnabled { get; set; }
    public bool LikesVisible { get; set; }
    public bool IsLiked { get; set; }
    public bool IsSaved { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<PostMediaDto> MediaFiles { get; set; } = new();
    public List<string> Hashtags { get; set; } = new();
    public List<UserSummaryDto> TaggedUsers { get; set; } = new();
}

public class PostMediaDto
{
    public Guid Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string MediaType { get; set; } = string.Empty;
    public int Order { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public double? Duration { get; set; }
}

public class CreatePostDto
{
    public string? Caption { get; set; }
    public string? Location { get; set; }
    public PostType PostType { get; set; } = PostType.Image;
    public bool CommentsEnabled { get; set; } = true;
    public bool LikesVisible { get; set; } = true;
    public PostVisibility Visibility { get; set; } = PostVisibility.Public;
    public List<IFormFile> MediaFiles { get; set; } = new();
    public List<string>? Hashtags { get; set; }
    public List<Guid>? TaggedUserIds { get; set; }
}

public class UpdatePostDto
{
    public string? Caption { get; set; }
    public string? Location { get; set; }
    public bool? CommentsEnabled { get; set; }
    public bool? LikesVisible { get; set; }
}
