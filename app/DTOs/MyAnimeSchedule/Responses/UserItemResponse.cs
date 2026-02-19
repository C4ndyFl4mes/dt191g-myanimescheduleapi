namespace App.DTOs;

public record UserItemResponse
{
    public required int UserID { get; set; }
    public required string Username { get; set; }
    public required string Role { get; set; }
    public required string TimeZone { get; set; }
}