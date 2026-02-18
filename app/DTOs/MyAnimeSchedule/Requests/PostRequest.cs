namespace App.DTOs;

public record PostRequest
{
    public int TargetID { get; set; }
    public required string Content { get; set; }
}