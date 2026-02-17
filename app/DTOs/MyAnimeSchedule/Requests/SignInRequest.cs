using System.ComponentModel.DataAnnotations;

namespace App.DTOs;

public record SignInRequest
{
    [Required, EmailAddress]
    public required string Email { get; set; }
    [Required, MinLength(6)]
    public required string Password { get; set; }
}