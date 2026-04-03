using FluentValidation;
using InstagramApi.Application.DTOs.Auth;
using InstagramApi.Application.DTOs.Comment;
using InstagramApi.Application.DTOs.Post;
using InstagramApi.Application.DTOs.User;

namespace InstagramApi.Application.Validators;

public class RegisterValidator : AbstractValidator<RegisterDto>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required")
            .MinimumLength(3).WithMessage("Username must be at least 3 characters")
            .MaximumLength(30).WithMessage("Username cannot exceed 30 characters")
            .Matches(@"^[a-zA-Z0-9._]+$").WithMessage("Username can only contain letters, numbers, dots and underscores");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches(@"\d").WithMessage("Password must contain at least one digit");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required")
            .MaximumLength(100).WithMessage("Full name cannot exceed 100 characters");
    }
}

public class LoginValidator : AbstractValidator<LoginDto>
{
    public LoginValidator()
    {
        RuleFor(x => x.UsernameOrEmail).NotEmpty().WithMessage("Username or email is required");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required");
    }
}

public class ForgotPasswordValidator : AbstractValidator<ForgotPasswordDto>
{
    public ForgotPasswordValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("Valid email is required");
    }
}

public class ResetPasswordValidator : AbstractValidator<ResetPasswordDto>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8)
            .Matches(@"[A-Z]")
            .Matches(@"[a-z]")
            .Matches(@"\d");
        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.NewPassword).WithMessage("Passwords do not match");
    }
}

public class ChangePasswordValidator : AbstractValidator<ChangePasswordDto>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8)
            .Matches(@"[A-Z]").Matches(@"[a-z]").Matches(@"\d");
        RuleFor(x => x.ConfirmPassword).Equal(x => x.NewPassword).WithMessage("Passwords do not match");
    }
}

public class UpdateProfileValidator : AbstractValidator<UpdateProfileDto>
{
    public UpdateProfileValidator()
    {
        RuleFor(x => x.FullName).MaximumLength(100).When(x => x.FullName != null);
        RuleFor(x => x.Bio).MaximumLength(150).When(x => x.Bio != null);
        RuleFor(x => x.Website).MaximumLength(200).When(x => x.Website != null);
    }
}

public class CreateCommentValidator : AbstractValidator<CreateCommentDto>
{
    public CreateCommentValidator()
    {
        RuleFor(x => x.Text)
            .NotEmpty().WithMessage("Comment text is required")
            .MaximumLength(2200).WithMessage("Comment cannot exceed 2200 characters");
    }
}

public class CreatePostValidator : AbstractValidator<CreatePostDto>
{
    public CreatePostValidator()
    {
        RuleFor(x => x.MediaFiles)
            .NotEmpty().WithMessage("At least one media file is required")
            .Must(f => f.Count <= 10).WithMessage("Maximum 10 media files allowed");

        RuleFor(x => x.Caption)
            .MaximumLength(2200).WithMessage("Caption cannot exceed 2200 characters")
            .When(x => x.Caption != null);
    }
}
