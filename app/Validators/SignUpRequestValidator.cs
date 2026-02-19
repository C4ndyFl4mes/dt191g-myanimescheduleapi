using System.Text.RegularExpressions;
using App.DTOs;
using FluentValidation;
using NodaTime;

namespace App.Validators;

public class SignUpRequestValidator : AbstractValidator<SignUpRequest>
{
    public SignUpRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
                .WithMessage("Username is required.")
            .MinimumLength(3)
                .WithMessage("Username cannot be shorter than 3 characters.")
            .MaximumLength(20)
                .WithMessage("Username cannot be longer than 20 characters.")
            .Must(NotContainHarmfulContent)
                .WithMessage("Content contains HTML or Script tags, which is not allowed.");

        RuleFor(x => x.Email)
            .NotEmpty()
                .WithMessage("Email is required.")
            .EmailAddress()
                .WithMessage("Email must be a valid email address.");

        RuleFor(x => x.Password)
            .NotEmpty()
                .WithMessage("Password is required.")
            .MinimumLength(16)
                .WithMessage("Password cannot be shorter than 16 characters.")
            .Custom((password, context) =>
            {
                if (!password.Any(char.IsUpper))
                    context.AddFailure("Password must contain at least one uppercase letter.");
                if (!password.Any(char.IsLower))
                    context.AddFailure("Password must contain at least one lowercase letter.");
                if (!password.Any(char.IsDigit))
                    context.AddFailure("Password must contain at least one digit.");
                if (!password.Any(c => !char.IsLetterOrDigit(c)))
                    context.AddFailure("Password must contain at least one special character.");
            });
        RuleFor(x => x.InitialSettings.TimeZone)
             .NotEmpty()
                .WithMessage("TimeZone is required.")
            .Must(timezone =>
            {
                try
                {
                    _ = DateTimeZoneProviders.Tzdb[timezone];
                    return true;
                }
                catch
                {
                    return false;
                }
            })
                .WithMessage("'{PropertyName}' is not a valid IANA timezone.");
    }

    // Skyddar från skadlig HTML.
    private bool NotContainHarmfulContent(string content)
    {
        if (string.IsNullOrEmpty(content))
            return true;

        // Content med HTML taggar förbjuds.
        return !Regex.IsMatch(content, @"<[^>]*>", RegexOptions.IgnoreCase);
    }
}