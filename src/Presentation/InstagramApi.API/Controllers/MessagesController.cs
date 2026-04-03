using AutoMapper;
using InstagramApi.Application.DTOs.Message;
using InstagramApi.Application.Interfaces.Repositories;
using InstagramApi.Application.Interfaces.Services;
using InstagramApi.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InstagramApi.API.Controllers;

[Authorize]
public class MessagesController : BaseController
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly IFileService _fileService;
    private readonly INotificationService _notificationService;

    public MessagesController(IUnitOfWork uow, IMapper mapper,
        IFileService fileService, INotificationService notificationService)
    {
        _uow = uow;
        _mapper = mapper;
        _fileService = fileService;
        _notificationService = notificationService;
    }

    /// <summary>Get all conversations for current user</summary>
    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations()
    {
        var conversations = await _uow.Messages.GetUserConversationsAsync(CurrentUserId);
        var dtos = new List<ConversationDto>();

        foreach (var conv in conversations)
        {
            var dto = _mapper.Map<ConversationDto>(conv);
            var lastMsg = conv.Messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault();
            if (lastMsg != null)
                dto.LastMessage = _mapper.Map<MessageDto>(lastMsg);

            dto.UnreadCount = conv.Messages.Count(m => m.ReceiverId == CurrentUserId && !m.IsRead);
            dtos.Add(dto);
        }

        return ApiOk(dtos);
    }

    /// <summary>Get messages in a conversation</summary>
    [HttpGet("conversations/{conversationId}/messages")]
    public async Task<IActionResult> GetMessages(Guid conversationId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 30)
    {
        var messages = await _uow.Messages.GetConversationMessagesAsync(conversationId, page, pageSize);

        // Mark as read
        foreach (var msg in messages.Where(m => m.ReceiverId == CurrentUserId && !m.IsRead))
        {
            msg.IsRead = true;
            msg.ReadAt = DateTime.UtcNow;
            await _uow.Messages.UpdateAsync(msg);
        }
        await _uow.SaveChangesAsync();

        var dtos = _mapper.Map<IEnumerable<MessageDto>>(messages);
        return ApiOk(dtos);
    }

    /// <summary>Send a message</summary>
    [HttpPost("send")]
    public async Task<IActionResult> SendMessage([FromForm] SendMessageDto dto)
    {
        if (dto.ReceiverId == CurrentUserId)
            return ApiBadRequest("Cannot send message to yourself");

        var receiver = await _uow.Users.GetByIdAsync(dto.ReceiverId);
        if (receiver == null) return ApiNotFound("Receiver not found");

        var isBlocked = await _uow.Users.IsBlockedAsync(dto.ReceiverId, CurrentUserId);
        if (isBlocked) return ApiForbidden("Cannot send message to this user");

        var conversation = await _uow.Messages.GetOrCreateConversationAsync(CurrentUserId, dto.ReceiverId);
        await _uow.SaveChangesAsync();

        string? mediaUrl = null;
        string? mediaType = null;

        if (dto.MediaFile != null)
        {
            if (_fileService.IsValidImageFile(dto.MediaFile))
            {
                mediaUrl = await _fileService.UploadImageAsync(dto.MediaFile, "messages");
                mediaType = "image";
            }
            else if (_fileService.IsValidVideoFile(dto.MediaFile))
            {
                mediaUrl = await _fileService.UploadVideoAsync(dto.MediaFile, "messages");
                mediaType = "video";
            }
        }

        var message = new Message
        {
            ConversationId = conversation!.Id,
            SenderId = CurrentUserId,
            ReceiverId = dto.ReceiverId,
            Text = dto.Text,
            MediaUrl = mediaUrl,
            MediaType = mediaType,
            ReplyToMessageId = dto.ReplyToMessageId
        };

        await _uow.Messages.AddAsync(message);

        conversation.LastMessageAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync();

        await _notificationService.SendNotificationAsync(dto.ReceiverId, CurrentUserId,
            Domain.Enums.NotificationType.DirectMessage,
            $"@{CurrentUser.Username} sent you a message", message.Id, "Message");

        return ApiCreated(_mapper.Map<MessageDto>(message), "Message sent");
    }

    /// <summary>Unsend (delete) a message</summary>
    [HttpDelete("{messageId}")]
    public async Task<IActionResult> UnsendMessage(Guid messageId)
    {
        var message = await _uow.Messages.GetByIdAsync(messageId);
        if (message == null) return ApiNotFound();
        if (message.SenderId != CurrentUserId) return ApiForbidden("Can only unsend your own messages");

        message.IsUnsent = true;
        message.Text = null;
        message.MediaUrl = null;

        await _uow.Messages.UpdateAsync(message);
        await _uow.SaveChangesAsync();

        return ApiOk("Message unsent");
    }

    /// <summary>Get unread message count</summary>
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var count = await _uow.Messages.GetUnreadCountAsync(CurrentUserId);
        return ApiOk(new { unreadCount = count });
    }
}
