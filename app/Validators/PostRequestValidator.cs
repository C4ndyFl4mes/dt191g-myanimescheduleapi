using System.Text.RegularExpressions;
using App.DTOs;
using FluentValidation;

namespace App.Validators;

public class PostRequestValidator : AbstractValidator<PostRequest>
{
    public PostRequestValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty()
                .WithMessage("Content is required.")
            .MinimumLength(10)
                .WithMessage("Content cannot be shorter than 10 characters.")
            .MaximumLength(500)
                .WithMessage("Content cannot be longer than 500 characters.")
            .Must(NotContainHarmfulContent)
                .WithMessage("Content contains HTML or Script tags, which is not allowed.");
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