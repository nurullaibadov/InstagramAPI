using InstagramApi.Application.DTOs.Auth;
using InstagramApi.Application.DTOs.User;
using Microsoft.AspNetCore.Http;

namespace InstagramApi.Application.Interfaces.Services;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
    Task<AuthResponseDto> RefreshTokenAsync(string refreshToken);
    Task LogoutAsync(Guid userId);
    Task ForgotPasswordAsync(ForgotPasswordDto dto);
    Task<bool> ResetPasswordAsync(ResetPasswordDto dto);
    Task<bool> ConfirmEmailAsync(string userId, string token);
    Task<bool> ResendEmailConfirmationAsync(string email);
    Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto);
}

public interface ITokenService
{
    string GenerateAccessToken(Domain.Entities.AppUser user, IList<string> roles);
    string GenerateRefreshToken();
    System.Security.Claims.ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
}

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body, bool isHtml = true);
    Task SendEmailConfirmationAsync(string email, string username, string token, Guid userId);
    Task SendPasswordResetAsync(string email, string username, string token);
    Task SendWelcomeEmailAsync(string email, string username);
    Task SendFollowNotificationAsync(string email, string username, string followerUsername);
}

public interface IFileService
{
    Task<string> UploadImageAsync(IFormFile file, string folder);
    Task<string> UploadVideoAsync(IFormFile file, string folder);
    Task DeleteFileAsync(string fileUrl);
    Task<string> ProcessAndUploadImageAsync(IFormFile file, string folder, int? maxWidth = null, int? maxHeight = null);
    bool IsValidImageFile(IFormFile file);
    bool IsValidVideoFile(IFormFile file);
}

public interface ICurrentUserService
{
    Guid UserId { get; }
    string? Username { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
    IEnumerable<string> Roles { get; }
    bool IsInRole(string role);
}

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
    Task RemoveAsync(string key);
    Task RemoveByPrefixAsync(string prefix);
    Task<bool> ExistsAsync(string key);
}

public interface INotificationService
{
    Task SendNotificationAsync(Guid userId, Guid? actorId, Domain.Enums.NotificationType type, 
        string message, Guid? referenceId = null, string? referenceType = null);
    Task SendPushNotificationAsync(Guid userId, string title, string body, object? data = null);
}
