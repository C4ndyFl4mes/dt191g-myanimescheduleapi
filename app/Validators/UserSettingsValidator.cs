using App.DTOs;
using FluentValidation;
using NodaTime;

namespace App.Validators;

public class UserSettingsValidator : AbstractValidator<UserSettings>
{
    public UserSettingsValidator()
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
        
        // Kommer lägga till detta senare när jag vet vad bild urlerna blir.
        // RuleFor(x => x.ProfileImageURL)
        //     .Must(url =>
        //     {
        //         if (url == null)
        //             return true;

        //         if (url.)
        //     });
    }
}