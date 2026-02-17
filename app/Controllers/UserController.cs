using App.DTOs;
using App.Extensions;
using App.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController(AuthService _authService, UserManagementService _userManagementService) : ControllerBase
{
    [HttpPost("signup")]
    public async Task<ActionResult<ProfileResponse>> SignUp(SignUpRequest request, IValidator<SignUpRequest> validator)
    {
        validator.ValidateAndThrow(request);

        ProfileResponse profile = await _authService.SignUp(request);

        return CreatedAtAction(nameof(SignUp), profile);
    }

    [HttpPost("signin")]
    public async Task<ActionResult<ProfileResponse>> SignIn(SignInRequest request, IValidator<SignInRequest> validator)
    {
        validator.ValidateAndThrow(request);

        ProfileResponse profile = await _authService.SignIn(request);

        return Ok(profile);
    }

    [HttpPost("signout"), Authorize]
    public async Task<ActionResult> LogOut()
    {
        await _authService.SignOut();

        return NoContent();
    }

    [HttpGet("profile"), Authorize]
    public async Task<ActionResult<ProfileResponse>> GetProfile()
    {
        ProfileResponse profile = await _userManagementService.Profile(User.GetUserID());

        return Ok(profile);
    }

    [HttpDelete("user/{targetID}"), Authorize(Roles = "Moderator")]
    public async Task<ActionResult<bool>> DeleteUser(int targetID)
    {
        await _userManagementService.DeleteUser(User.GetUserID(), targetID);

        return NoContent();
    }
}