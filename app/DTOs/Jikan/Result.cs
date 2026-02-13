namespace App.DTOs;

public record Result
{
    public required Pagination pagination { get; set; }
    public required List<IndexableAnime> data { get; set; } = [];
}

public record Pagination
{
    public required int last_visible_page { get; set; }
    public required bool has_next_page { get; set; }
    public required int current_page { get; set; }
    public required Items items { get; set; }
}

public record Items
{
    public required int count { get; set; }
    public required int total { get; set; }
    public required int per_page { get; set; }
}