using App.Data;
using App.DTOs;
using App.Exceptions;
using App.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace App.Services;

public class PostManagementService(ApplicationDbContext _context, UserManager<UserModel> _userManager)
{
    // Hämtar alla poster tillhörande en indexerad anime.
    public async Task<DataPaginatedResponse<PostResponse>> GetTargetPosts(PostGetRequest request)
    {
        IndexedAnimeModel? indexedAnime = await _context.IndexedAnimes.FindAsync(request.TargetID) ??
            throw new NotFoundException("Target thread not available.");

        // Beräknar antalet poster som tillhör en viss anime.
        int totalCount = await _context.Posts
            .Where(p => p.AnimeId == indexedAnime.Id)
            .CountAsync();

        // Använder en anonym lista för att sedan kunna lägga till rätt lokal datum och tid.
        var posts = await _context.Posts
        .Include(p => p.Author)
        .Where(p => p.AnimeId == indexedAnime.Id)
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

        // Hämtar DateTimeZone genom en sträng. ! pga i query i controller måste den vara string, därför är denna inte null.
        DateTimeZone timeZone = DateTimeZoneProviders.Tzdb[request.TimeZone!];
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

        return new DataPaginatedResponse<PostResponse>
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
    }

    // Skapar en post. En användare kan lägga upp poster till en indexerad anime.
    public async Task SendToTarget(PostRequest request, int userID)
    {
        IndexedAnimeModel? indexedAnime = await _context.IndexedAnimes.FindAsync(request.TargetID) ??
            throw new NotFoundException("Target thread not available.");

        UserModel? user = await _context.Users.FindAsync(userID) ??
            throw new NotFoundException("User does not exist.");

        PostModel post = new()
        {
            AuthorId = user.Id,
            AnimeId = indexedAnime.Id,
            Content = request.Content,
            CreatedAt = SystemClock.Instance.GetCurrentInstant()
        };

        _context.Posts.Add(post);

        await _context.SaveChangesAsync();
    }

    // Uppdaterar en post. Alla användare kan bara ändra sin egen post.
    public async Task UpdateTargetPost(PostRequest request, int userID)
    {
        PostModel? post = await _context.Posts.FindAsync(request.TargetID) ??
            throw new NotFoundException("Target post does not exist.");

        UserModel? user = await _context.Users.FindAsync(userID) ??
            throw new NotFoundException("User does not exist.");

        if (post.AuthorId != user.Id)
            throw new UnauthorizedException("You are not allowed to edit another user's post.");

        post.Content = request.Content;

        if (_context.ChangeTracker.HasChanges())
        {
            await _context.SaveChangesAsync();
        }
    }

    // Raderar en post. En moderator kan radera andras poster, men en member kan bara radera sin egen.
    public async Task DeleteTargetPost(int userID, int targetID)
    {
        PostModel? post = await _context.Posts.FindAsync(targetID) ??
            throw new NotFoundException("Target post does not exist.");

        UserModel? user = await _context.Users.FindAsync(userID) ??
            throw new NotFoundException("User does not exist.");

        IList<string> roles = await _userManager.GetRolesAsync(user);

        if (!roles.Contains("Moderator") && user.Id != post.AuthorId)
            throw new UnauthorizedException("You are not allowed to delete this user's post.");

        _context.Remove(post);

        if (_context.ChangeTracker.HasChanges())
        {
            await _context.SaveChangesAsync();
        }
    }
}