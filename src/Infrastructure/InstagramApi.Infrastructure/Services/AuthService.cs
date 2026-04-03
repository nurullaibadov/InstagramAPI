using InstagramApi.Application.DTOs.Auth;
using InstagramApi.Application.DTOs.User;
using InstagramApi.Application.Interfaces.Repositories;
using InstagramApi.Application.Interfaces.Services;
using InstagramApi.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using AutoMapper;

namespace InstagramApi.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IUnitOfWork _uow;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;
    private readonly IMapper _mapper;

    public AuthService(UserManager<AppUser> userManager, IUnitOfWork uow,
        ITokenService tokenService, IEmailService emailService, IMapper mapper)
    {
        _userManager = userManager;
        _uow = uow;
        _tokenService = tokenService;
        _emailService = emailService;
        _mapper = mapper;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        if (await _userManager.FindByEmailAsync(dto.Email) != null)
            throw new InvalidOperationException("Email already in use");

        if (await _userManager.FindByNameAsync(dto.Username) != null)
            throw new InvalidOperationException("Username already taken");

        var user = new AppUser
        {
            UserName = dto.Username.ToLower(),
            Email = dto.Email.ToLower(),
            FullName = dto.FullName,
            DateOfBirth = dto.DateOfBirth,
            PhoneNumber = dto.PhoneNumber
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));

        await _userManager.AddToRoleAsync(user, "User");

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        user.EmailConfirmationToken = token;
        await _userManager.UpdateAsync(user);

        await _emailService.SendEmailConfirmationAsync(user.Email!, user.UserName!, token, user.Id);
        await _emailService.SendWelcomeEmailAsync(user.Email!, user.UserName!);

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var refreshToken = _tokenService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(30);
        await _userManager.UpdateAsync(user);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            User = _mapper.Map<UserDto>(user)
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var user = dto.UsernameOrEmail.Contains('@')
            ? await _userManager.FindByEmailAsync(dto.UsernameOrEmail)
            : await _userManager.FindByNameAsync(dto.UsernameOrEmail);

        if (user == null || user.IsDeleted)
            throw new UnauthorizedAccessException("Invalid credentials");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is deactivated");

        if (!await _userManager.CheckPasswordAsync(user, dto.Password))
            throw new UnauthorizedAccessException("Invalid credentials");

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var refreshToken = _tokenService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(dto.RememberMe ? 60 : 30);
        await _userManager.UpdateAsync(user);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            User = _mapper.Map<UserDto>(user)
        };
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
    {
        var user = await _uow.Users.GetAsync(u => u.RefreshToken == refreshToken);

        if (user == null || user.RefreshTokenExpiry < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Invalid or expired refresh token");

        var roles = await _userManager.GetRolesAsync(user);
        var newAccessToken = _tokenService.GenerateAccessToken(user, roles);
        var newRefreshToken = _tokenService.GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(30);
        await _userManager.UpdateAsync(user);

        return new AuthResponseDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            User = _mapper.Map<UserDto>(user)
        };
    }

    public async Task LogoutAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user != null)
        {
            user.RefreshToken = null;
            user.RefreshTokenExpiry = null;
            await _userManager.UpdateAsync(user);
        }
    }

    public async Task ForgotPasswordAsync(ForgotPasswordDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null) return; // Silent fail for security

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        user.PasswordResetToken = token;
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(2);
        await _userManager.UpdateAsync(user);

        await _emailService.SendPasswordResetAsync(user.Email!, user.UserName!, token);
    }

    public async Task<bool> ResetPasswordAsync(ResetPasswordDto dto)
    {
        var user = await _userManager.FindByIdAsync(dto.UserId);
        if (user == null) return false;

        var result = await _userManager.ResetPasswordAsync(user, dto.Token, dto.NewPassword);
        if (result.Succeeded)
        {
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;
            user.RefreshToken = null;
            await _userManager.UpdateAsync(user);
        }
        return result.Succeeded;
    }

    public async Task<bool> ConfirmEmailAsync(string userId, string token)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return false;

        var result = await _userManager.ConfirmEmailAsync(user, token);
        return result.Succeeded;
    }

    public async Task<bool> ResendEmailConfirmationAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null || user.EmailConfirmed) return false;

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        await _emailService.SendEmailConfirmationAsync(user.Email!, user.UserName!, token, user.Id);
        return true;
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new KeyNotFoundException("User not found");

        var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
    }
}
