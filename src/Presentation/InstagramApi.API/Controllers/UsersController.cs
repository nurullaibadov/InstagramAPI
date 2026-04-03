using AutoMapper;
using InstagramApi.Application.Common;
using InstagramApi.Application.DTOs.User;
using InstagramApi.Application.Interfaces.Repositories;
using InstagramApi.Application.Interfaces.Services;
using InstagramApi.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InstagramApi.API.Controllers;

[Authorize]
public class UsersController : BaseController
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly IFileService _fileService;
    private readonly INotificationService _notificationService;

    public UsersController(IUnitOfWork uow, IMapper mapper,
        IFileService fileService, INotificationService notificationService)
    {
        _uow = uow;
        _mapper = mapper;
        _fileService = fileService;
        _notificationService = notificationService;
    }

    /// <summary>Get user profile by username</summary>
    [HttpGet("{username}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProfile(string username)
    {
        var user = await _uow.Users.GetByUsernameAsync(username);
        if (user == null) return ApiNotFound("User not found");

        var dto = _mapper.Map<UserDto>(user);

        if (CurrentUser.IsAuthenticated && CurrentUserId != user.Id)
        {
            dto.IsFollowing = await _uow.Users.IsFollowingAsync(CurrentUserId, user.Id);
            dto.IsFollowedBy = await _uow.Users.IsFollowingAsync(user.Id, CurrentUserId);
            dto.IsBlocked = await _uow.Users.IsBlockedAsync(CurrentUserId, user.Id);
        }

        return ApiOk(dto);
    }

    /// <summary>Get current user's profile</summary>
    [HttpGet("profile/me")]
    public async Task<IActionResult> GetMyProfile()
    {
        var user = await _uow.Users.GetByIdAsync(CurrentUserId);
        if (user == null) return ApiNotFound();

        var dto = _mapper.Map<UserProfileDto>(user);
        return ApiOk(dto);
    }

    /// <summary>Update current user's profile</summary>
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        var user = await _uow.Users.GetByIdAsync(CurrentUserId);
        if (user == null) return ApiNotFound();

        if (dto.FullName != null) user.FullName = dto.FullName;
        if (dto.Bio != null) user.Bio = dto.Bio;
        if (dto.Website != null) user.Website = dto.Website;
        if (dto.IsPrivate.HasValue) user.IsPrivate = dto.IsPrivate.Value;
        if (dto.Gender != null) user.Gender = dto.Gender;
        if (dto.DateOfBirth.HasValue) user.DateOfBirth = dto.DateOfBirth;
        if (dto.PhoneNumber != null) user.PhoneNumber = dto.PhoneNumber;

        await _uow.Users.UpdateAsync(user);
        await _uow.SaveChangesAsync();

        return ApiOk(_mapper.Map<UserDto>(user), "Profile updated");
    }

    /// <summary>Upload profile picture</summary>
    [HttpPost("profile/avatar")]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        if (!_fileService.IsValidImageFile(file))
            return ApiBadRequest("Invalid image file");

        var user = await _uow.Users.GetByIdAsync(CurrentUserId);
        if (user == null) return ApiNotFound();

        if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
            await _fileService.DeleteFileAsync(user.ProfilePictureUrl);

        var url = await _fileService.ProcessAndUploadImageAsync(file, "avatars", 300, 300);
        user.ProfilePictureUrl = url;

        await _uow.Users.UpdateAsync(user);
        await _uow.SaveChangesAsync();

        return ApiOk(new { profilePictureUrl = url }, "Avatar updated");
    }

    /// <summary>Search users</summary>
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (string.IsNullOrWhiteSpace(q)) return ApiBadRequest("Search query is required");

        var users = await _uow.Users.SearchUsersAsync(q, page, pageSize);
        var dtos = new List<UserSummaryDto>();

        foreach (var u in users)
        {
            var dto = _mapper.Map<UserSummaryDto>(u);
            dto.IsFollowing = await _uow.Users.IsFollowingAsync(CurrentUserId, u.Id);
            dtos.Add(dto);
        }

        return ApiOk(dtos);
    }

    /// <summary>Get suggested users to follow</summary>
    [HttpGet("suggestions")]
    public async Task<IActionResult> GetSuggestions([FromQuery] int count = 10)
    {
        var users = await _uow.Users.GetSuggestedUsersAsync(CurrentUserId, count);
        var dtos = _mapper.Map<IEnumerable<UserSummaryDto>>(users);
        return ApiOk(dtos);
    }

    /// <summary>Follow a user</summary>
    [HttpPost("{userId}/follow")]
    public async Task<IActionResult> Follow(Guid userId)
    {
        if (userId == CurrentUserId) return ApiBadRequest("Cannot follow yourself");

        var target = await _uow.Users.GetByIdAsync(userId);
        if (target == null) return ApiNotFound("User not found");

        var isBlocked = await _uow.Users.IsBlockedAsync(userId, CurrentUserId);
        if (isBlocked) return ApiForbidden("You cannot follow this user");

        var existing = await _uow.Follows.GetFollowAsync(CurrentUserId, userId);
        if (existing != null) return ApiBadRequest("Already following this user");

        var follow = new Follow
        {
            FollowerId = CurrentUserId,
            FollowingId = userId,
            IsAccepted = !target.IsPrivate
        };

        await _uow.Follows.AddAsync(follow);

        if (!target.IsPrivate)
        {
            target.FollowersCount++;
            var currentUser = await _uow.Users.GetByIdAsync(CurrentUserId);
            if (currentUser != null) currentUser.FollowingCount++;
            await _uow.Users.UpdateAsync(target);
        }

        await _uow.SaveChangesAsync();

        var notifMsg = target.IsPrivate ? "sent you a follow request" : "started following you";
        var notifType = target.IsPrivate
            ? Domain.Enums.NotificationType.FollowRequest
            : Domain.Enums.NotificationType.Follow;

        await _notificationService.SendNotificationAsync(userId, CurrentUserId, notifType,
            $"@{CurrentUser.Username} {notifMsg}", CurrentUserId, "User");

        return ApiOk(target.IsPrivate ? "Follow request sent" : "Following");
    }

    /// <summary>Unfollow a user</summary>
    [HttpDelete("{userId}/follow")]
    public async Task<IActionResult> Unfollow(Guid userId)
    {
        var follow = await _uow.Follows.GetFollowAsync(CurrentUserId, userId);
        if (follow == null) return ApiBadRequest("Not following this user");

        await _uow.Follows.DeleteAsync(follow);

        var target = await _uow.Users.GetByIdAsync(userId);
        var me = await _uow.Users.GetByIdAsync(CurrentUserId);

        if (target != null && follow.IsAccepted)
        {
            target.FollowersCount = Math.Max(0, target.FollowersCount - 1);
            await _uow.Users.UpdateAsync(target);
        }

        if (me != null && follow.IsAccepted)
        {
            me.FollowingCount = Math.Max(0, me.FollowingCount - 1);
            await _uow.Users.UpdateAsync(me);
        }

        await _uow.SaveChangesAsync();
        return ApiOk("Unfollowed");
    }

    /// <summary>Accept a follow request</summary>
    [HttpPost("follow-requests/{followerId}/accept")]
    public async Task<IActionResult> AcceptFollowRequest(Guid followerId)
    {
        var follow = await _uow.Follows.GetFollowAsync(followerId, CurrentUserId);
        if (follow == null || follow.IsAccepted) return ApiNotFound("Follow request not found");

        follow.IsAccepted = true;

        var target = await _uow.Users.GetByIdAsync(CurrentUserId);
        var requester = await _uow.Users.GetByIdAsync(followerId);

        if (target != null) { target.FollowersCount++; await _uow.Users.UpdateAsync(target); }
        if (requester != null) { requester.FollowingCount++; await _uow.Users.UpdateAsync(requester); }

        await _uow.Follows.UpdateAsync(follow);
        await _uow.SaveChangesAsync();

        await _notificationService.SendNotificationAsync(followerId, CurrentUserId,
            Domain.Enums.NotificationType.FollowAccepted,
            $"@{CurrentUser.Username} accepted your follow request", CurrentUserId, "User");

        return ApiOk("Follow request accepted");
    }

    /// <summary>Decline a follow request</summary>
    [HttpDelete("follow-requests/{followerId}/decline")]
    public async Task<IActionResult> DeclineFollowRequest(Guid followerId)
    {
        var follow = await _uow.Follows.GetFollowAsync(followerId, CurrentUserId);
        if (follow == null) return ApiNotFound("Follow request not found");

        await _uow.Follows.DeleteAsync(follow);
        await _uow.SaveChangesAsync();
        return ApiOk("Follow request declined");
    }

    /// <summary>Get pending follow requests</summary>
    [HttpGet("follow-requests")]
    public async Task<IActionResult> GetFollowRequests()
    {
        var requests = await _uow.Follows.GetPendingRequestsAsync(CurrentUserId);
        var dtos = requests.Select(f => _mapper.Map<UserSummaryDto>(f.Follower));
        return ApiOk(dtos);
    }

    /// <summary>Get followers list</summary>
    [HttpGet("{userId}/followers")]
    public async Task<IActionResult> GetFollowers(Guid userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 30)
    {
        var users = await _uow.Users.GetFollowersAsync(userId, page, pageSize);
        var dtos = new List<UserSummaryDto>();
        foreach (var u in users)
        {
            var dto = _mapper.Map<UserSummaryDto>(u);
            dto.IsFollowing = await _uow.Users.IsFollowingAsync(CurrentUserId, u.Id);
            dtos.Add(dto);
        }
        return ApiOk(dtos);
    }

    /// <summary>Get following list</summary>
    [HttpGet("{userId}/following")]
    public async Task<IActionResult> GetFollowing(Guid userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 30)
    {
        var users = await _uow.Users.GetFollowingAsync(userId, page, pageSize);
        var dtos = new List<UserSummaryDto>();
        foreach (var u in users)
        {
            var dto = _mapper.Map<UserSummaryDto>(u);
            dto.IsFollowing = await _uow.Users.IsFollowingAsync(CurrentUserId, u.Id);
            dtos.Add(dto);
        }
        return ApiOk(dtos);
    }

    /// <summary>Block a user</summary>
    [HttpPost("{userId}/block")]
    public async Task<IActionResult> Block(Guid userId)
    {
        if (userId == CurrentUserId) return ApiBadRequest("Cannot block yourself");

        var isAlreadyBlocked = await _uow.Users.IsBlockedAsync(CurrentUserId, userId);
        if (isAlreadyBlocked) return ApiBadRequest("User already blocked");

        // Remove follow relationships
        var followByMe = await _uow.Follows.GetFollowAsync(CurrentUserId, userId);
        var followByThem = await _uow.Follows.GetFollowAsync(userId, CurrentUserId);

        if (followByMe != null) await _uow.Follows.DeleteAsync(followByMe);
        if (followByThem != null) await _uow.Follows.DeleteAsync(followByThem);

        var block = new BlockedUser { BlockerId = CurrentUserId, BlockedId = userId };
        await _uow.BlockedUsers.AddAsync(block);
        await _uow.SaveChangesAsync();

        return ApiOk("User blocked");
    }

    /// <summary>Unblock a user</summary>
    [HttpDelete("{userId}/block")]
    public async Task<IActionResult> Unblock(Guid userId)
    {
        var block = await _uow.BlockedUsers.GetAsync(b => b.BlockerId == CurrentUserId && b.BlockedId == userId);
        if (block == null) return ApiBadRequest("User is not blocked");

        await _uow.BlockedUsers.DeleteAsync(block);
        await _uow.SaveChangesAsync();
        return ApiOk("User unblocked");
    }

    /// <summary>Delete account (soft delete)</summary>
    [HttpDelete("account")]
    public async Task<IActionResult> DeleteAccount()
    {
        var user = await _uow.Users.GetByIdAsync(CurrentUserId);
        if (user == null) return ApiNotFound();

        user.IsDeleted = true;
        user.IsActive = false;
        user.DeletedAt = DateTime.UtcNow;
        user.RefreshToken = null;

        await _uow.Users.UpdateAsync(user);
        await _uow.SaveChangesAsync();
        return ApiOk("Account deleted");
    }
}
