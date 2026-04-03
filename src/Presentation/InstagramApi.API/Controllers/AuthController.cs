using InstagramApi.Application.DTOs.Auth;
using InstagramApi.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace InstagramApi.API.Controllers;

[SwaggerTag("Authentication endpoints")]
public class AuthController : BaseController
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService) => _authService = authService;

    /// <summary>Register a new user</summary>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var result = await _authService.RegisterAsync(dto);
        return ApiCreated(result, "Registration successful. Please check your email to confirm.");
    }

    /// <summary>Login with username/email and password</summary>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var result = await _authService.LoginAsync(dto);
        return ApiOk(result, "Login successful");
    }

    /// <summary>Refresh access token using refresh token</summary>
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto dto)
    {
        var result = await _authService.RefreshTokenAsync(dto.RefreshToken);
        return ApiOk(result, "Token refreshed");
    }

    /// <summary>Logout current user</summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _authService.LogoutAsync(CurrentUserId);
        return ApiOk("Logged out successfully");
    }

    /// <summary>Send password reset email</summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        await _authService.ForgotPasswordAsync(dto);
        return ApiOk("If an account exists with this email, a reset link has been sent.");
    }

    /// <summary>Reset password using token from email</summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        var success = await _authService.ResetPasswordAsync(dto);
        if (!success) return ApiBadRequest("Invalid or expired reset token");
        return ApiOk("Password reset successfully");
    }

    /// <summary>Confirm email with token</summary>
    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string token)
    {
        var success = await _authService.ConfirmEmailAsync(userId, token);
        if (!success) return ApiBadRequest("Invalid or expired confirmation token");
        return ApiOk("Email confirmed successfully");
    }

    /// <summary>Resend email confirmation</summary>
    [HttpPost("resend-confirmation")]
    public async Task<IActionResult> ResendConfirmation([FromBody] ForgotPasswordDto dto)
    {
        await _authService.ResendEmailConfirmationAsync(dto.Email);
        return ApiOk("Confirmation email resent if account exists");
    }

    /// <summary>Change password for authenticated user</summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        await _authService.ChangePasswordAsync(CurrentUserId, dto);
        return ApiOk("Password changed successfully");
    }

    /// <summary>Get current authenticated user info</summary>
    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        return ApiOk(new
        {
            id = CurrentUser.UserId,
            username = CurrentUser.Username,
            email = CurrentUser.Email,
            roles = CurrentUser.Roles
        });
    }
}
