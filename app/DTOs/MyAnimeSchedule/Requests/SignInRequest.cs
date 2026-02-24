using System.ComponentModel.DataAnnotations;

namespace App.DTOs;

public record SignInRequest
{
    public required string Email { get; set; }
    public required string Password { get; set; }
}