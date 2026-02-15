using Microsoft.AspNetCore.Identity;
using NodaTime;

namespace App.Models;

public class UserModel : IdentityUser<int>
{
    public string? ProfileImageURL { get; set; }
    public bool ShowExplicitAnime { get; set; } = false;
    public bool AllowReminders { get; set; } = false;
    public string TimeZoneID { get; set; } = DateTimeZoneProviders.Tzdb.GetSystemDefault().Id;
}