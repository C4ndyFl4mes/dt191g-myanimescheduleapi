namespace App.DTOs;

// DTO för att skicka tillbaka användarprofilinformation efter inloggning eller registrering
public record ProfileResponse
{
    public string? Username { get; set; }
    public string? Role { get; set; }
    // public string? ProfileImageURL { get; set; }
    // public bool ShowExplicitAnime { get; set; }
    // public bool AllowReminders { get; set; }
    public required UserSettings Settings { get; set; }
}