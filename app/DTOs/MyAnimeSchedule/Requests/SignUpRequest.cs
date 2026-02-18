namespace App.DTOs;

public record SignUpRequest
{
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required string TimeZone { get; set; }
}