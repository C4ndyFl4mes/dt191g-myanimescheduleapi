using System.ComponentModel.DataAnnotations;

namespace App.DTOs;

// DTO för att ta emot inloggnings- och registreringsdata
public record CredentialsRequest
{
    [Required, MinLength(3), MaxLength(20)]
    public required string Username { get; set; }
    [Required, EmailAddress]
    public required string Email { get; set; }
    [Required, MinLength(6)]
    public required string Password { get; set; }
}