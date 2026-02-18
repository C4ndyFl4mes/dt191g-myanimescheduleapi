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

    [HttpDelete("{targetID}"), Authorize(Roles = "Moderator")]
    public async Task<ActionResult> DeleteUser(int targetID)
    {
        await _userManagementService.DeleteUser(User.GetUserID(), targetID);

        return NoContent();
    }

    [HttpGet("info/{page}"), Authorize]
    public async Task<ActionResult<UserInfoResponse>> GetUserInfo([FromQuery] int? targetID, [FromQuery] string? timezone, int page, IValidator<PostGetRequest> validator)
    {
        PostGetRequest request = new(){ TargetID = targetID, Page = page, TimeZone = timezone};
        
        validator.ValidateAndThrow(request);

        UserInfoResponse userInfo = await _userManagementService.GetUserInfo(User.GetUserID(), request);

        return Ok(userInfo);    
    }

    [HttpPut("settings"), Authorize]
    public async Task<ActionResult<UserSettings>> SetSettings(UserSettings settings, IValidator<UserSettings> validator)
    {
        validator.ValidateAndThrow(settings);
        
        UserSettings? updatedSettings = await _userManagementService.SetSettings(User.GetUserID(), settings);

        if (updatedSettings == null) 
            return NoContent();

        return Ok(updatedSettings);
    }
}