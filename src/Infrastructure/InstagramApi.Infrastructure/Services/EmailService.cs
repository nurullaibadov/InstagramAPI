using InstagramApi.Application.Interfaces.Services;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace InstagramApi.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config) => _config = config;

    public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_config["Email:SenderName"], _config["Email:SenderEmail"]));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;

        var builder = new BodyBuilder();
        if (isHtml) builder.HtmlBody = body;
        else builder.TextBody = body;
        message.Body = builder.ToMessageBody();

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(_config["Email:SmtpHost"], int.Parse(_config["Email:SmtpPort"] ?? "587"),
            SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(_config["Email:Username"], _config["Email:Password"]);
        await smtp.SendAsync(message);
        await smtp.DisconnectAsync(true);
    }

    public async Task SendEmailConfirmationAsync(string email, string username, string token, Guid userId)
    {
        var baseUrl = _config["App:BaseUrl"];
        var encodedToken = Uri.EscapeDataString(token);
        var confirmUrl = $"{baseUrl}/api/auth/confirm-email?userId={userId}&token={encodedToken}";

        var body = $@"
        <html><body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
            <div style='background: linear-gradient(135deg, #405de6, #5851db, #833ab4, #c13584, #e1306c, #fd1d1d); padding: 40px; text-align: center;'>
                <h1 style='color: white; font-size: 32px;'>📸 InstagramAPI</h1>
            </div>
            <div style='padding: 40px; background: #fff;'>
                <h2>Hello {username}! 👋</h2>
                <p>Thank you for registering. Please confirm your email address:</p>
                <a href='{confirmUrl}' style='display: inline-block; background: #405de6; color: white; padding: 12px 30px; border-radius: 8px; text-decoration: none; font-weight: bold;'>
                    Confirm Email
                </a>
                <p style='color: #666; margin-top: 20px; font-size: 14px;'>This link expires in 24 hours.</p>
            </div>
        </body></html>";

        await SendEmailAsync(email, "Confirm your email - InstagramAPI", body);
    }

    public async Task SendPasswordResetAsync(string email, string username, string token)
    {
        var baseUrl = _config["App:FrontendUrl"];
        var encodedToken = Uri.EscapeDataString(token);
        var resetUrl = $"{baseUrl}/reset-password?token={encodedToken}&email={Uri.EscapeDataString(email)}";

        var body = $@"
        <html><body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
            <div style='background: linear-gradient(135deg, #405de6, #833ab4, #e1306c); padding: 40px; text-align: center;'>
                <h1 style='color: white;'>🔐 Password Reset</h1>
            </div>
            <div style='padding: 40px; background: #fff;'>
                <h2>Hello {username},</h2>
                <p>You requested to reset your password. Click the button below:</p>
                <a href='{resetUrl}' style='display: inline-block; background: #e1306c; color: white; padding: 12px 30px; border-radius: 8px; text-decoration: none; font-weight: bold;'>
                    Reset Password
                </a>
                <p style='color: #666; margin-top: 20px; font-size: 14px;'>This link expires in 2 hours. If you didn't request this, ignore this email.</p>
            </div>
        </body></html>";

        await SendEmailAsync(email, "Password Reset - InstagramAPI", body);
    }

    public async Task SendWelcomeEmailAsync(string email, string username)
    {
        var body = $@"
        <html><body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
            <div style='background: linear-gradient(135deg, #405de6, #833ab4, #e1306c); padding: 40px; text-align: center;'>
                <h1 style='color: white; font-size: 32px;'>Welcome to InstagramAPI! 🎉</h1>
            </div>
            <div style='padding: 40px; background: #fff;'>
                <h2>Hey {username}! 🌟</h2>
                <p>Your account is ready. Start sharing your moments with the world!</p>
                <ul>
                    <li>📸 Share photos and videos</li>
                    <li>🎥 Create Reels and Stories</li>
                    <li>💬 Connect with friends</li>
                    <li>🔍 Explore trending content</li>
                </ul>
            </div>
        </body></html>";

        await SendEmailAsync(email, $"Welcome {username}! - InstagramAPI", body);
    }

    public async Task SendFollowNotificationAsync(string email, string username, string followerUsername)
    {
        var body = $@"<html><body style='font-family: Arial, sans-serif;'>
            <p>Hi {username}, <strong>{followerUsername}</strong> started following you on InstagramAPI!</p>
        </body></html>";

        await SendEmailAsync(email, $"{followerUsername} started following you", body);
    }
}
