namespace App.DTOs;

public record UserInfoResponse
{
    public required ProfileResponse Profile { get; set; }
    public required DataPaginatedResponse<PostResponse> Activity { get; set; }
}