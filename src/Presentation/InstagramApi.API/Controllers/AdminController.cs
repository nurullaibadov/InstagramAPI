using AutoMapper;
using InstagramApi.Application.DTOs.Admin;
using InstagramApi.Application.Interfaces.Repositories;
using InstagramApi.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace InstagramApi.API.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
[Route("api/admin")]
[ApiController]
[Produces("application/json")]
public class AdminController : BaseController
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly UserManager<AppUser> _userManager;

    public AdminController(IUnitOfWork uow, IMapper mapper, UserManager<AppUser> userManager)
    {
        _uow = uow;
        _mapper = mapper;
        _userManager = userManager;
    }

    // ===================== DASHBOARD =====================

    /// <summary>Get admin dashboard statistics</summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var weekAgo = today.AddDays(-7);

        var allUsers = await _uow.Users.GetAllAsync();
        var allPosts = await _uow.Posts.GetAllAsync();
        var allReports = await _uow.Reports.GetAllAsync();

        var dailyStats = new List<DailyStatsDto>();
        for (int i = 6; i >= 0; i--)
        {
            var day = today.AddDays(-i);
            var nextDay = day.AddDays(1);
            dailyStats.Add(new DailyStatsDto
            {
                Date = day,
                NewUsers = allUsers.Count(u => u.CreatedAt >= day && u.CreatedAt < nextDay),
                NewPosts = allPosts.Count(p => p.CreatedAt >= day && p.CreatedAt < nextDay),
                NewReports = allReports.Count(r => r.CreatedAt >= day && r.CreatedAt < nextDay)
            });
        }

        var dashboard = new AdminDashboardDto
        {
            TotalUsers = allUsers.Count(),
            NewUsersToday = allUsers.Count(u => u.CreatedAt >= today),
            NewUsersThisWeek = allUsers.Count(u => u.CreatedAt >= weekAgo),
            TotalPosts = allPosts.Count(),
            NewPostsToday = allPosts.Count(p => p.CreatedAt >= today),
            TotalReports = allReports.Count(),
            PendingReports = allReports.Count(r => r.Status == Domain.Enums.ReportStatus.Pending),
            ActiveUsers = allUsers.Count(u => u.IsActive),
            DailyStats = dailyStats
        };

        return ApiOk(dashboard);
    }

    // ===================== USER MANAGEMENT =====================

    /// <summary>Get all users with pagination</summary>
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null, [FromQuery] bool? isActive = null)
    {
        var query = await _uow.Users.GetQueryableAsync();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(u => u.UserName!.Contains(search) || u.Email!.Contains(search) || u.FullName.Contains(search));

        if (isActive.HasValue)
            query = query.Where(u => u.IsActive == isActive.Value);

        var totalCount = query.Count();
        var users = query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var dtos = new List<AdminUserDto>();
        foreach (var u in users)
        {
            var dto = _mapper.Map<AdminUserDto>(u);
            dto.Roles = (await _userManager.GetRolesAsync(u)).ToList();
            dtos.Add(dto);
        }

        return ApiOk(Application.Common.PaginatedResponse<AdminUserDto>.Create(dtos, page, pageSize, totalCount));
    }

    /// <summary>Get a specific user</summary>
    [HttpGet("users/{userId}")]
    public async Task<IActionResult> GetUser(Guid userId)
    {
        var user = await _uow.Users.GetByIdAsync(userId);
        if (user == null) return ApiNotFound("User not found");

        var dto = _mapper.Map<AdminUserDto>(user);
        dto.Roles = (await _userManager.GetRolesAsync(user)).ToList();

        return ApiOk(dto);
    }

    /// <summary>Update user status, verification, or role</summary>
    [HttpPatch("users/{userId}")]
    public async Task<IActionResult> UpdateUser(Guid userId, [FromBody] AdminUpdateUserDto dto)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return ApiNotFound("User not found");

        if (dto.IsActive.HasValue)
            user.IsActive = dto.IsActive.Value;

        if (dto.IsVerified.HasValue)
            user.IsVerified = dto.IsVerified.Value;

        await _userManager.UpdateAsync(user);

        if (!string.IsNullOrEmpty(dto.Role))
        {
            var currentRoles = await _userManager.GetRolesAsync(user);
            // Keep "User" role, just change elevated roles
            var elevatedRoles = new[] { "Moderator", "Admin", "SuperAdmin" };
            foreach (var r in elevatedRoles)
                if (currentRoles.Contains(r))
                    await _userManager.RemoveFromRoleAsync(user, r);

            if (dto.Role != "User")
                await _userManager.AddToRoleAsync(user, dto.Role);
        }

        return ApiOk("User updated");
    }

    /// <summary>Ban (deactivate) a user</summary>
    [HttpPost("users/{userId}/ban")]
    public async Task<IActionResult> BanUser(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return ApiNotFound();

        user.IsActive = false;
        user.RefreshToken = null;
        await _userManager.UpdateAsync(user);
        await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);

        return ApiOk("User banned");
    }

    /// <summary>Unban (reactivate) a user</summary>
    [HttpPost("users/{userId}/unban")]
    public async Task<IActionResult> UnbanUser(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return ApiNotFound();

        user.IsActive = true;
        await _userManager.UpdateAsync(user);
        await _userManager.SetLockoutEndDateAsync(user, null);

        return ApiOk("User unbanned");
    }

    /// <summary>Permanently delete a user</summary>
    [HttpDelete("users/{userId}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> DeleteUser(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return ApiNotFound();

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
            return ApiBadRequest(string.Join(", ", result.Errors.Select(e => e.Description)));

        return ApiOk("User permanently deleted");
    }

    // ===================== POST MANAGEMENT =====================

    /// <summary>Get all posts with pagination</summary>
    [HttpGet("posts")]
    public async Task<IActionResult> GetPosts([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _uow.Posts.GetPagedAsync(page, pageSize,
            orderBy: q => q.OrderByDescending(p => p.CreatedAt));

        var dtos = result.Items.Select(p => new
        {
            p.Id, p.Caption, p.LikesCount, p.CommentsCount, p.CreatedAt,
            UserId = p.UserId, p.IsDeleted
        });

        return ApiOk(new { items = dtos, result.TotalCount, result.TotalPages, result.Page });
    }

    /// <summary>Delete any post (admin)</summary>
    [HttpDelete("posts/{postId}")]
    public async Task<IActionResult> DeletePost(Guid postId)
    {
        var post = await _uow.Posts.GetByIdAsync(postId);
        if (post == null) return ApiNotFound();

        await _uow.Posts.SoftDeleteAsync(post);
        await _uow.SaveChangesAsync();

        return ApiOk("Post deleted by admin");
    }

    // ===================== REPORTS =====================

    /// <summary>Get pending reports</summary>
    [HttpGet("reports")]
    public async Task<IActionResult> GetReports([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var reports = await _uow.Reports.GetPendingReportsAsync(page, pageSize);
        var dtos = _mapper.Map<IEnumerable<ReportDto>>(reports);
        return ApiOk(dtos);
    }

    /// <summary>Resolve a report</summary>
    [HttpPatch("reports/{reportId}")]
    public async Task<IActionResult> ResolveReport(Guid reportId, [FromBody] ResolveReportDto dto)
    {
        var report = await _uow.Reports.GetByIdAsync(reportId);
        if (report == null) return ApiNotFound("Report not found");

        report.Status = dto.Status;
        report.AdminNote = dto.AdminNote;
        report.ReviewedByAdminId = CurrentUserId;
        report.ReviewedAt = DateTime.UtcNow;

        await _uow.Reports.UpdateAsync(report);
        await _uow.SaveChangesAsync();

        return ApiOk("Report resolved");
    }

    // ===================== SETTINGS =====================

    /// <summary>Get application settings</summary>
    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings()
    {
        var settings = await _uow.AppSettings.GetAllAsync();
        var result = settings.ToDictionary(s => s.Key, s => s.Value);
        return ApiOk(result);
    }

    /// <summary>Update an application setting</summary>
    [HttpPut("settings/{key}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> UpdateSetting(string key, [FromBody] string value)
    {
        var setting = await _uow.AppSettings.GetAsync(s => s.Key == key);

        if (setting == null)
        {
            setting = new AppSettings { Key = key, Value = value };
            await _uow.AppSettings.AddAsync(setting);
        }
        else
        {
            setting.Value = value;
            await _uow.AppSettings.UpdateAsync(setting);
        }

        await _uow.SaveChangesAsync();
        return ApiOk("Setting updated");
    }
}
