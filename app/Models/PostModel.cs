using System.ComponentModel.DataAnnotations;
using NodaTime;

namespace App.Models;

public class PostModel
{
    public int Id { get; set; }
    public required int AuthorId { get; set; }
    public required int AnimeId { get; set; }
    public UserModel? Author { get; set; }
    public IndexedAnimeModel? Anime { get; set; }

    [Required, MinLength(10), MaxLength(500)]
    public required string Content { get; set; }
    public required Instant CreatedAt { get; set; }
}