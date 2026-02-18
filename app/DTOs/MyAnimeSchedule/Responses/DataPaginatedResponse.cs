namespace App.DTOs;

public record DataPaginatedResponse<T>
{
    public required Pagination Pagination { get; set; }
    public required List<T> Data { get; set; }
}