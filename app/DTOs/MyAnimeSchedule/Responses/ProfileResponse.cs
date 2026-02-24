namespace App.DTOs;

// DTO för att skicka tillbaka användarprofilinformation efter inloggning eller registrering
public record ProfileResponse
{
    public string? Token { get; set; }
    public string? Username { get; set; }
    public string? Role { get; set; }
    public required UserSettings Settings { get; set; }
}