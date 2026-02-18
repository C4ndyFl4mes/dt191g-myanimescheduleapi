namespace App.DTOs;

public record UserSettings
{
    public string? ProfileImageURL { get; set; }
    public required bool ShowExplicitAnime { get; set; }
    public required bool AllowReminders { get; set; }
    public required string TimeZone { get; set; }
}