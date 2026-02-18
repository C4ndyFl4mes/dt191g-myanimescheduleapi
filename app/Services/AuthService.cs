using App.DTOs;
using App.Exceptions;
using App.Models;
using Microsoft.AspNetCore.Identity;

namespace App.Services;

public class AuthService(UserManager<UserModel> _userManager, SignInManager<UserModel> _signInManager)
{
    // Skapar en användare.
    public async Task<ProfileResponse> SignUp(SignUpRequest request)
    {
        UserModel? userEmail = await _userManager.FindByEmailAsync(request.Email);
        UserModel? userName = await _userManager.FindByNameAsync(request.Username);

        bool exists = userEmail != null || userName != null;

        // Om användarnamn och e-post är tillgänglig kan en ny användare skapas.
        if (!exists)
        {
            UserModel user = new()
            {
                UserName = request.Username,
                Email = request.Email,
                TimeZoneID = request.InitialSettings.TimeZone,
                ShowExplicitAnime = request.InitialSettings.ShowExplicitAnime,
                AllowReminders = request.InitialSettings.AllowReminders
            };

            // Skapar användaren.
            IdentityResult result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
                throw new BadRequestException($"User creation failed: {string.Join(", ", result.Errors.Select(e => e.Description))}");

            // Tilldela roll.
            IdentityResult roleResult = await _userManager.AddToRoleAsync(user, "Member");
            if (!roleResult.Succeeded)
            {
                // Raderar användaren om rolltilldelningen misslyckades.
                await _userManager.DeleteAsync(user);
                throw new InternalServerException($"Failed to assing role: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
            }

            // Loggar in direkt efter registrering.
            return await SignIn(new()
            {
                Email = request.Email,
                Password = request.Password
            });
        }

        // Detta kan vara dåligt då det visar att en användare med det angivna användarnamnet eller e-posten redan finns i databasen.
        throw new ConflictException("This user already exists.");
    }

    // Loggar in en användare.
    public async Task<ProfileResponse> SignIn(SignInRequest request)
    {
        UserModel? user = await _userManager.FindByEmailAsync(request.Email) ??
            throw new UnauthorizedException("Invalid credentials.");

        var result = await _signInManager.PasswordSignInAsync(user, request.Password, isPersistent: false, lockoutOnFailure: false);
        if (!result.Succeeded)
            throw new UnauthorizedException("Invalid credentials.");

        IList<string>? roles = await _userManager.GetRolesAsync(user);
        string role = roles.FirstOrDefault() ?? "Member";

        return new ProfileResponse
        {
            Username = user.UserName!,
            Role = role,
            Settings = new()
            {
                ProfileImageURL = user.ProfileImageURL,
                ShowExplicitAnime = user.ShowExplicitAnime,
                AllowReminders = user.AllowReminders,
                TimeZone = user.TimeZoneID
            }
        };
    }

    // Loggar ut en användare.
    public async Task SignOut()
    {
        await _signInManager.SignOutAsync();
    }
}