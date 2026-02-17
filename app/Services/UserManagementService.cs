using App.DTOs;
using App.Exceptions;
using App.Models;
using Microsoft.AspNetCore.Identity;

namespace App.Services;

public class UserManagementService(UserManager<UserModel> _userManager)
{
    // Hämtar den inloggade användarens profil.
    public async Task<ProfileResponse> Profile(int userID)
    {
        UserModel? user = await _userManager.FindByIdAsync(userID.ToString()) ??
            throw new NotFoundException("User not found.");

        IList<string>? roles = await _userManager.GetRolesAsync(user);
        string role = roles.FirstOrDefault() ?? "Member";

        return new ProfileResponse
        {
            Username = user.UserName!,
            Role = role,
            ProfileImageURL = user.ProfileImageURL,
            ShowExplicitAnime = user.ShowExplicitAnime,
            AllowReminders = user.AllowReminders
        };
    }

    // Moderator användare kan radera en Member användare.
    public async Task DeleteUser(int userID, int targetID)
    {
        UserModel? user = await _userManager.FindByIdAsync(userID.ToString()) ??
            throw new NotFoundException("User not found.");

        IList<string>? myRoles = await _userManager.GetRolesAsync(user);
        if (!myRoles.Contains("Moderator"))
            throw new UnauthorizedException("Only moderators can perform this action.");

        if (userID == targetID)
            throw new BadRequestException("You cannot delete yourself.");

        UserModel? targetUser = await _userManager.FindByIdAsync(targetID.ToString()) ??
            throw new NotFoundException("The target user not found.");

        IList<string>? targetRoles = await _userManager.GetRolesAsync(targetUser);
        if (targetRoles.Contains("Moderator"))
            throw new UnauthorizedException("You are not allowed to delete another moderator.");

        await _userManager.DeleteAsync(targetUser);
    }
}