using System.ComponentModel.DataAnnotations;

namespace App.Models;

public class PostModel
{
    public int Id { get; set; }
    public required UserModel Author { get; set; }
    public required IndexedAnimeModel Anime { get; set; }

    [Required, MinLength(10), MaxLength(500)]
    public required string Content { get; set; }
    public required DateTime CreatedAt { get; set; }
}