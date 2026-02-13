namespace App.Models;

public sealed class IndexedAnimeModel
{
    public int Id { get; set; }
    public required int Mal_ID { get; set; }
    public required DateTime AutomaticRemovalDate { get; set; }
}