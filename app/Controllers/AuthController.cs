

using App.DTOs;
using App.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace App.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<UserModel> _userManager;
    private readonly SignInManager<UserModel> _signInManager;

    public AuthController(UserManager<UserModel> userManager, SignInManager<UserModel> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [HttpPost("signup")]
    public async Task<ActionResult<ProfileResponse>> SignUp(CredentialsRequest request)
    {
        // Validerar att användarnamn och email inte är tomma eller bara whitespace
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { Message = "Username and email are required." });
        }

        // Kollar om email redan används
        if (await _userManager.FindByEmailAsync(request.Email) != null)
        {
            return BadRequest(new { Message = "Email is already in use." });
        }

        // Kollar om användarnamn redan används
        if (await _userManager.FindByNameAsync(request.Username) != null)
        {
            return BadRequest(new { Message = "Username is already taken." });
        }

        UserModel user = new()
        {
            UserName = request.Username,
            Email = request.Email
        };

        IdentityResult result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            string errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return BadRequest(new { Message = $"User creation failed: {errors}" });
        }

        // Tilldela roll
        IdentityResult roleResult = await _userManager.AddToRoleAsync(user, "Member");
        if (!roleResult.Succeeded)
        {
            // Överväg att ta bort användaren om rolltilldelningen misslyckas
            await _userManager.DeleteAsync(user);
            string errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
            return StatusCode(500, new { Message = $"Failed to assign role: {errors}" });
        }

        return CreatedAtAction(nameof(SignUp), new ProfileResponse
        {
            Username = user.UserName!,
            Role = "Member",
            ProfileImageURL = user.ProfileImageURL,
            ShowExplicitAnime = user.ShowExplicitAnime,
            AllowReminders = user.AllowReminders
        });
    }

    [HttpPost("signin")]
    public async Task<ActionResult<ProfileResponse>> SignIn(CredentialsRequest request)
    {
        // Validerar att användarnamn och email inte är tomma eller bara whitespace
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { Message = "Username and email are required." });
        }

        UserModel? user = await _userManager.FindByEmailAsync(request.Email);

        // Kollar att användaren finns och att användarnamnet matchar det som skickats in.
        if (user == null)
        {
            return Unauthorized(new { Message = "Invalid credentials." });
        }

        if (user.UserName != request.Username)
        {
            return Unauthorized(new { Message = "Invalid credentials." });
        }

        var result = await _signInManager.PasswordSignInAsync(user, request.Password, isPersistent: false, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            return Unauthorized(new { Message = "Invalid credentials." });
        }

        IList<string>? roles = await _userManager.GetRolesAsync(user);
        string role = roles.FirstOrDefault() ?? "Member";

        return Ok(new ProfileResponse
        {
            Username = user.UserName!,
            Role = role,
            ProfileImageURL = user.ProfileImageURL,
            ShowExplicitAnime = user.ShowExplicitAnime,
            AllowReminders = user.AllowReminders
        });
    }

    [HttpGet("profile"), Authorize]
    public async Task<ActionResult<ProfileResponse>> GetProfile()
    {
        string? userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { Message = "User is not authenticated." });
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound(new { Message = "User not found." });
        }

        IList<string>? roles = await _userManager.GetRolesAsync(user);
        string role = roles.FirstOrDefault() ?? "Member";

        return Ok(new ProfileResponse
        {
            Username = user.UserName!,
            Role = role,
            ProfileImageURL = user.ProfileImageURL,
            ShowExplicitAnime = user.ShowExplicitAnime,
            AllowReminders = user.AllowReminders
        });

    }

}