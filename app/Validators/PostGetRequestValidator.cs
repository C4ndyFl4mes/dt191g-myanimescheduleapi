using App.DTOs;
using FluentValidation;
using NodaTime;

namespace App.Validators;

public class PostGetRequestValidator : AbstractValidator<PostGetRequest>
{
    public PostGetRequestValidator()
    {
        RuleFor(x => x.TimeZone)
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
}