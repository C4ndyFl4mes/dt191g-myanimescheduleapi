using Microsoft.AspNetCore.Identity;

namespace App.Models;

public class UserModel : IdentityUser<int>
{
    public string? ProfileImageURL { get; set; }
    public bool ShowExplicitAnime { get; set; } = false;
    public bool AllowReminders { get; set; } = false;
}