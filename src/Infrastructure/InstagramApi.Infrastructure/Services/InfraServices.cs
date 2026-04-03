using InstagramApi.Application.Interfaces.Repositories;
using InstagramApi.Application.Interfaces.Services;
using InstagramApi.Domain.Entities;
using InstagramApi.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Security.Claims;
using System.Text.Json;

namespace InstagramApi.Infrastructure.Services;

public class FileService : IFileService
{
    private readonly IConfiguration _config;
    private readonly string _uploadPath;

    public FileService(IConfiguration config)
    {
        _config = config;
        _uploadPath = _config["FileStorage:LocalPath"] ?? "wwwroot/uploads";
        Directory.CreateDirectory(_uploadPath);
    }

    public async Task<string> UploadImageAsync(IFormFile file, string folder)
    {
        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var dirPath = Path.Combine(_uploadPath, folder);
        Directory.CreateDirectory(dirPath);
        var filePath = Path.Combine(dirPath, fileName);

        using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        var baseUrl = _config["App:BaseUrl"];
        return $"{baseUrl}/uploads/{folder}/{fileName}";
    }

    public async Task<string> ProcessAndUploadImageAsync(IFormFile file, string folder,
        int? maxWidth = null, int? maxHeight = null)
    {
        var fileName = $"{Guid.NewGuid()}.jpg";
        var dirPath = Path.Combine(_uploadPath, folder);
        Directory.CreateDirectory(dirPath);
        var filePath = Path.Combine(dirPath, fileName);

        using var image = await Image.LoadAsync(file.OpenReadStream());

        if (maxWidth.HasValue || maxHeight.HasValue)
        {
            var width = maxWidth ?? image.Width;
            var height = maxHeight ?? image.Height;
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(width, height),
                Mode = ResizeMode.Max
            }));
        }

        await image.SaveAsJpegAsync(filePath);

        var baseUrl = _config["App:BaseUrl"];
        return $"{baseUrl}/uploads/{folder}/{fileName}";
    }

    public async Task<string> UploadVideoAsync(IFormFile file, string folder)
        => await UploadImageAsync(file, folder); // same logic for videos

    public Task DeleteFileAsync(string fileUrl)
    {
        var baseUrl = _config["App:BaseUrl"];
        var relativePath = fileUrl.Replace(baseUrl!, "").TrimStart('/');
        var fullPath = Path.Combine("wwwroot", relativePath);

        if (File.Exists(fullPath)) File.Delete(fullPath);
        return Task.CompletedTask;
    }

    public bool IsValidImageFile(IFormFile file)
    {
        var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var ext = Path.GetExtension(file.FileName).ToLower();
        return allowed.Contains(ext) && file.Length <= 10 * 1024 * 1024; // 10MB
    }

    public bool IsValidVideoFile(IFormFile file)
    {
        var allowed = new[] { ".mp4", ".mov", ".avi", ".webm" };
        var ext = Path.GetExtension(file.FileName).ToLower();
        return allowed.Contains(ext) && file.Length <= 100 * 1024 * 1024; // 100MB
    }
}

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUserService(IHttpContextAccessor accessor) => _accessor = accessor;

    public Guid UserId
    {
        get
        {
            var id = _accessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return id != null ? Guid.Parse(id) : Guid.Empty;
        }
    }

    public string? Username => _accessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Name);
    public string? Email => _accessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email);
    public bool IsAuthenticated => _accessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
    public IEnumerable<string> Roles => _accessor.HttpContext?.User?.Claims
        .Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value) ?? Enumerable.Empty<string>();
    public bool IsInRole(string role) => _accessor.HttpContext?.User?.IsInRole(role) ?? false;
}

public class CacheService : ICacheService
{
    private readonly IDistributedCache _cache;

    public CacheService(IDistributedCache cache) => _cache = cache;

    public async Task<T?> GetAsync<T>(string key)
    {
        var data = await _cache.GetStringAsync(key);
        return data == null ? default : JsonSerializer.Deserialize<T>(data);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        var opts = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiry ?? TimeSpan.FromMinutes(30)
        };
        await _cache.SetStringAsync(key, JsonSerializer.Serialize(value), opts);
    }

    public async Task RemoveAsync(string key) => await _cache.RemoveAsync(key);

    public Task RemoveByPrefixAsync(string prefix)
    {
        // For Redis, you would use SCAN + DEL. For memory cache, not trivially doable here.
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(string key)
    {
        var data = await _cache.GetStringAsync(key);
        return data != null;
    }
}

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _uow;

    public NotificationService(IUnitOfWork uow) => _uow = uow;

    public async Task SendNotificationAsync(Guid userId, Guid? actorId, NotificationType type,
        string message, Guid? referenceId = null, string? referenceType = null)
    {
        var notification = new Notification
        {
            UserId = userId,
            ActorId = actorId,
            Type = type,
            Message = message,
            ReferenceId = referenceId,
            ReferenceType = referenceType
        };

        await _uow.Notifications.AddAsync(notification);
        await _uow.SaveChangesAsync();
    }

    public Task SendPushNotificationAsync(Guid userId, string title, string body, object? data = null)
    {
        // Integrate FCM/APNs here
        return Task.CompletedTask;
    }
}
