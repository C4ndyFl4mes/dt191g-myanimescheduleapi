using App.DTOs;
using FluentValidation;

namespace App.Validators;

public class SignInRequestValidator : AbstractValidator<SignInRequest>
{
    public SignInRequestValidator()
    {
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
    }
}