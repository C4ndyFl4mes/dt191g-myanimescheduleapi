using NodaTime;

namespace App.DTOs;

public record PostResponse
{
    public required int postID { get; set; }
    public required int AuthorID { get; set; }
    public required string AuthorName { get; set; }
    public required string Content { get; set; }
    public required string LocalDateTime { get; set; }
}