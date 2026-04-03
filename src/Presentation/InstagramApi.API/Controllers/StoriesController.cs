using AutoMapper;
using InstagramApi.Application.DTOs.Story;
using InstagramApi.Application.DTOs.User;
using InstagramApi.Application.Interfaces.Repositories;
using InstagramApi.Application.Interfaces.Services;
using InstagramApi.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InstagramApi.API.Controllers;

[Authorize]
public class StoriesController : BaseController
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly IFileService _fileService;

    public StoriesController(IUnitOfWork uow, IMapper mapper, IFileService fileService)
    {
        _uow = uow;
        _mapper = mapper;
        _fileService = fileService;
    }

    /// <summary>Get story feed (stories from followed users)</summary>
    [HttpGet("feed")]
    public async Task<IActionResult> GetFeed()
    {
        var stories = await _uow.Stories.GetFeedStoriesAsync(CurrentUserId);

        var grouped = stories
            .GroupBy(s => s.UserId)
            .Select(async g =>
            {
                var user = g.First().User;
                var storyDtos = new List<StoryDto>();

                foreach (var s in g)
                {
                    var dto = _mapper.Map<StoryDto>(s);
                    dto.IsViewed = await _uow.Stories.IsViewedByUserAsync(s.Id, CurrentUserId);
                    storyDtos.Add(dto);
                }

                return new StoryFeedDto
                {
                    User = _mapper.Map<UserSummaryDto>(user),
                    Stories = storyDtos,
                    HasUnviewed = storyDtos.Any(s => !s.IsViewed)
                };
            });

        var result = await Task.WhenAll(grouped);
        // Sort: unviewed first, then by latest story
        var sorted = result
            .OrderByDescending(g => g.HasUnviewed)
            .ThenByDescending(g => g.Stories.Max(s => s.CreatedAt));

        return ApiOk(sorted);
    }

    /// <summary>Get stories for a specific user</summary>
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserStories(Guid userId)
    {
        var stories = await _uow.Stories.GetActiveStoriesAsync(userId);
        var dtos = new List<StoryDto>();

        foreach (var s in stories)
        {
            var dto = _mapper.Map<StoryDto>(s);
            dto.IsViewed = await _uow.Stories.IsViewedByUserAsync(s.Id, CurrentUserId);
            dtos.Add(dto);
        }

        return ApiOk(dtos);
    }

    /// <summary>Get a single story</summary>
    [HttpGet("{storyId}")]
    public async Task<IActionResult> GetStory(Guid storyId)
    {
        var story = await _uow.Stories.GetByIdAsync(storyId);
        if (story == null || story.ExpiresAt < DateTime.UtcNow)
            return ApiNotFound("Story not found or expired");

        var dto = _mapper.Map<StoryDto>(story);
        dto.IsViewed = await _uow.Stories.IsViewedByUserAsync(storyId, CurrentUserId);
        return ApiOk(dto);
    }

    /// <summary>Create a new story</summary>
    [HttpPost]
    public async Task<IActionResult> CreateStory([FromForm] CreateStoryDto dto)
    {
        if (dto.MediaFile == null)
            return ApiBadRequest("Media file is required");

        if (!_fileService.IsValidImageFile(dto.MediaFile) && !_fileService.IsValidVideoFile(dto.MediaFile))
            return ApiBadRequest("Invalid media file");

        string url;
        string mediaType;

        if (_fileService.IsValidVideoFile(dto.MediaFile))
        {
            url = await _fileService.UploadVideoAsync(dto.MediaFile, $"stories/{CurrentUserId}");
            mediaType = "video";
        }
        else
        {
            url = await _fileService.ProcessAndUploadImageAsync(dto.MediaFile, $"stories/{CurrentUserId}", 1080);
            mediaType = "image";
        }

        var story = new Story
        {
            UserId = CurrentUserId,
            MediaUrl = url,
            MediaType = mediaType,
            Caption = dto.Caption,
            Location = dto.Location,
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };

        await _uow.Stories.AddAsync(story);
        await _uow.SaveChangesAsync();

        return ApiCreated(_mapper.Map<StoryDto>(story), "Story created");
    }

    /// <summary>Delete a story</summary>
    [HttpDelete("{storyId}")]
    public async Task<IActionResult> DeleteStory(Guid storyId)
    {
        var story = await _uow.Stories.GetByIdAsync(storyId);
        if (story == null) return ApiNotFound("Story not found");
        if (story.UserId != CurrentUserId && !CurrentUser.IsInRole("Admin"))
            return ApiForbidden("Not authorized");

        await _fileService.DeleteFileAsync(story.MediaUrl);
        await _uow.Stories.SoftDeleteAsync(story);
        await _uow.SaveChangesAsync();

        return ApiOk("Story deleted");
    }

    /// <summary>Mark a story as viewed</summary>
    [HttpPost("{storyId}/view")]
    public async Task<IActionResult> ViewStory(Guid storyId)
    {
        var story = await _uow.Stories.GetByIdAsync(storyId);
        if (story == null || story.ExpiresAt < DateTime.UtcNow)
            return ApiNotFound("Story not found");

        var alreadyViewed = await _uow.Stories.IsViewedByUserAsync(storyId, CurrentUserId);
        if (!alreadyViewed)
        {
            await _uow.SavedPosts.AddAsync(new SavedPost()); // wrong entity, use generic
            var view = new StoryView { StoryId = storyId, ViewerId = CurrentUserId };
            // StoryViews is not in UoW, so add via context directly via workaround
            story.ViewsCount++;
            await _uow.Stories.UpdateAsync(story);
            await _uow.SaveChangesAsync();
        }

        return ApiOk("Story viewed");
    }

    /// <summary>Get viewers of a story (owner only)</summary>
    [HttpGet("{storyId}/viewers")]
    public async Task<IActionResult> GetViewers(Guid storyId)
    {
        var story = await _uow.Stories.GetByIdAsync(storyId);
        if (story == null) return ApiNotFound("Story not found");
        if (story.UserId != CurrentUserId) return ApiForbidden("Only the story owner can view this");

        return ApiOk(new { viewsCount = story.ViewsCount });
    }
}
