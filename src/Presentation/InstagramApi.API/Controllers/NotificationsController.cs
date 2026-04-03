using AutoMapper;
using InstagramApi.Application.DTOs.Notification;
using InstagramApi.Application.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InstagramApi.API.Controllers;

[Authorize]
public class NotificationsController : BaseController
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public NotificationsController(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    /// <summary>Get notifications for current user</summary>
    [HttpGet]
    public async Task<IActionResult> GetNotifications([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var notifications = await _uow.Notifications.GetUserNotificationsAsync(CurrentUserId, page, pageSize);
        var dtos = _mapper.Map<IEnumerable<NotificationDto>>(notifications);
        return ApiOk(dtos);
    }

    /// <summary>Get unread notification count</summary>
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var count = await _uow.Notifications.GetUnreadCountAsync(CurrentUserId);
        return ApiOk(new { unreadCount = count });
    }

    /// <summary>Mark a notification as read</summary>
    [HttpPatch("{notificationId}/read")]
    public async Task<IActionResult> MarkAsRead(Guid notificationId)
    {
        var notification = await _uow.Notifications.GetByIdAsync(notificationId);
        if (notification == null) return ApiNotFound("Notification not found");
        if (notification.UserId != CurrentUserId) return ApiForbidden();

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;

        await _uow.Notifications.UpdateAsync(notification);
        await _uow.SaveChangesAsync();

        return ApiOk("Marked as read");
    }

    /// <summary>Mark all notifications as read</summary>
    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        await _uow.Notifications.MarkAllAsReadAsync(CurrentUserId);
        return ApiOk("All notifications marked as read");
    }

    /// <summary>Delete a notification</summary>
    [HttpDelete("{notificationId}")]
    public async Task<IActionResult> DeleteNotification(Guid notificationId)
    {
        var notification = await _uow.Notifications.GetByIdAsync(notificationId);
        if (notification == null) return ApiNotFound();
        if (notification.UserId != CurrentUserId) return ApiForbidden();

        await _uow.Notifications.DeleteAsync(notification);
        await _uow.SaveChangesAsync();

        return ApiOk("Notification deleted");
    }
}
