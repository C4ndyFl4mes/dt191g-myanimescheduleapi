using System.Security.Claims;
using App.Exceptions;

namespace App.Extensions;

// En statisk klass för att hantera extensions.
public static class Extensions
{
    // Returnerar userID beroende på vem som är inloggad.
    public static int GetUserID(this ClaimsPrincipal claims)
    {
        string? userId = claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!int.TryParse(userId, out int numUserId)) throw new UnauthorizedException("Invalid user.");

        return numUserId;
    }
}