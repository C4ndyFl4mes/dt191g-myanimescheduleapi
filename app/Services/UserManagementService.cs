using App.Data;
using App.DTOs;
using App.Exceptions;
using App.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace App.Services;

public class UserManagementService(UserManager<UserModel> _userManager, ApplicationDbContext _context)
{
    // Moderator användare kan radera en Member användare.
    public async Task DeleteUser(int userID, int targetID)
    {
        UserModel? user = await _userManager.FindByIdAsync(userID.ToString()) ??
            throw new NotFoundException("User not found.");

        IList<string> myRoles = await _userManager.GetRolesAsync(user);
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

    // Uppdaterar användarens inställningar.
    public async Task<UserSettings?> SetSettings(int userID, UserSettings settings)
    {
        UserModel? user = await _userManager.FindByIdAsync(userID.ToString()) ??
            throw new NotFoundException("User not found.");

        if (
            user.AllowReminders == settings.AllowReminders &&
            user.ShowExplicitAnime == settings.ShowExplicitAnime &&
            user.ProfileImageURL == settings.ProfileImageURL &&
            user.TimeZoneID == settings.TimeZone
        )
        {
            return null;
        }

        user.AllowReminders = settings.AllowReminders;
        user.ShowExplicitAnime = settings.ShowExplicitAnime;
        user.ProfileImageURL = settings.ProfileImageURL;
        user.TimeZoneID = settings.TimeZone;

        await _userManager.UpdateAsync(user);
        return settings;
    }

    // Hämtar en användares infromation och aktiviteter. En moderator kan se andra användare.
    public async Task<UserInfoResponse> GetUserInfo(int userID, PostGetRequest request)
    {
        UserModel? user = await _userManager.FindByIdAsync(userID.ToString()) ??
            throw new NotFoundException("User not found.");

        IList<string> myRoles = await _userManager.GetRolesAsync(user);
        if (request.TargetID != null && !myRoles.Contains("Moderator"))
            throw new UnauthorizedException("You are not allowed to see another user's information.");

        // Om targetID är null, kommer man se sin egen information istället.
        request.TargetID ??= user.Id;
        UserModel? targetUser = request.TargetID != userID ? await _userManager.FindByIdAsync(request.TargetID.ToString()!) ??
            throw new NotFoundException("Target user not found.") : user;

        IList<string> targetRoles = request.TargetID != userID ? await _userManager.GetRolesAsync(targetUser) : myRoles;
        string role = targetRoles.FirstOrDefault() ?? "Member";

        ProfileResponse profile = new()
        {
            Username = targetUser.UserName!,
            Role = role,
            Settings = new()
            {
                ProfileImageURL = targetUser.ProfileImageURL,
                ShowExplicitAnime = targetUser.ShowExplicitAnime,
                AllowReminders = targetUser.AllowReminders,
                TimeZone = targetUser.TimeZoneID
            }

        };

        // Beräknar antalet poster som tillhör en viss användare.
        int totalCount = await _context.Posts
            .Where(p => p.AuthorId == targetUser.Id)
            .CountAsync();

        // Använder en anonym lista för att sedan kunna lägga till rätt lokal datum och tid.
        var posts = await _context.Posts
        .Include(p => p.Anime)
        .Where(p => p.AuthorId == targetUser.Id)
        .OrderByDescending(p => p.CreatedAt)
        .Skip((request.Page - 1) * request.PerPage)
        .Take(request.PerPage)
        .Select(p => new
        {
            postID = p.Id,
            AuthorID = p.AuthorId,
            AuthorName = p.Author!.UserName!,
            Content = p.Content,
            CreatedAt = p.CreatedAt
        }).ToListAsync();


        request.TimeZone ??= user.TimeZoneID;

        DateTimeZone timeZone = DateTimeZoneProviders.Tzdb[request.TimeZone]; // Hämtar DateTimeZone genom en sträng.
        List<PostResponse> convertedPosts = posts
            .Select(p => new PostResponse
            {
                postID = p.postID,
                AuthorID = p.AuthorID,
                AuthorName = p.AuthorName,
                Content = p.Content,
                LocalDateTime = $"{p.CreatedAt.InZone(timeZone).LocalDateTime.Date} {p.CreatedAt.InZone(timeZone).LocalDateTime.TimeOfDay}"
            }).ToList();


        int lastPage = (int)Math.Floor((double)totalCount / request.PerPage) + 1;

        DataPaginatedResponse<PostResponse> activity = new()
        {
            Pagination = new()
            {
                last_visible_page = lastPage,
                has_next_page = request.Page < lastPage,
                current_page = request.Page,
                items = new()
                {
                    count = 0,
                    total = totalCount,
                    per_page = request.PerPage
                }
            },
            Data = convertedPosts
        };

        return new UserInfoResponse
        {
            Profile = profile,
            Activity = activity
        };
    }

    // Hämtar alla användare i en paginerad lista.
    public async Task<DataPaginatedResponse<UserItemResponse>> GetUserList(int page, int perPage = 10)
    {
        int totalCount = await _userManager.Users.CountAsync();

         if (totalCount == 0)
            throw new NotFoundException("Det finns inga användare. Hur kunde du hamna här?");

        List<UserModel> usersData = await _userManager.Users
            .OrderByDescending(u => u.UserName)
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .ToListAsync();

        List<UserItemResponse> users = [];
        foreach (UserModel user in usersData)
        {
            users.Add(new UserItemResponse
            {
                UserID = user.Id,
                Username = user.UserName!,
                Role = await GetRole(user),
                TimeZone = user.TimeZoneID
            });
        }

        int lastPage = (int)Math.Floor((double)totalCount / perPage) + 1;

        return new DataPaginatedResponse<UserItemResponse>
        {
            Pagination = new()
            {
                last_visible_page = lastPage,
                has_next_page = page < lastPage,
                current_page = page,
                items = new()
                {
                    count = 0,
                    total = totalCount,
                    per_page = perPage
                }
            },
            Data = users
        };
    }

    // Hämtar användarroll för en användare i användarlistan.
    private async Task<string> GetRole(UserModel user)
    {
        IList<string> roles = await _userManager.GetRolesAsync(user);
        return roles.FirstOrDefault() ?? "Member";
    }
}