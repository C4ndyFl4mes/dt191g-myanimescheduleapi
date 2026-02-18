using App.DTOs;
using App.Extensions;
using App.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Controllers;

[ApiController, Route("api/[controller]")]
public class PostsController(PostManagementService _postManagementService) : ControllerBase
{
    [HttpGet("{targetID}/{page}/{timezone}")]
    public async Task<ActionResult> GetTargetPosts(int targetID, int page, string timezone)
    {
        PostGetRequest request = new(){ TargetID = targetID, Page = page, TimeZone = timezone.Replace("_", "/")};

        DataPaginatedResponse<PostResponse> data = await _postManagementService.GetTargetPosts(request);

        return Ok(data);
    }

    [HttpPost("send"), Authorize]
    public async Task<ActionResult> SendToTarget(PostRequest request, IValidator<PostRequest> validator)
    {
        validator.ValidateAndThrow(request);

        await _postManagementService.SendToTarget(request, User.GetUserID());

        return CreatedAtAction(nameof(SendToTarget), new { message = "Successfully sent post to the target thread." });
    }

    [HttpPut("edit"), Authorize]
    public async Task<ActionResult> UpdateTargetPost(PostRequest request, IValidator<PostRequest> validator)
    {
        validator.ValidateAndThrow(request);

        await _postManagementService.UpdateTargetPost(request, User.GetUserID());

        return Ok(new { message = "Successfully edited the target post." });
    }

    [HttpDelete("delete/{targetID}"), Authorize]
    public async Task<ActionResult> DeleteTargetPost(int targetID)
    {
        await _postManagementService.DeleteTargetPost(User.GetUserID(), targetID);

        return Ok(new { message = "Successfully deleted the target post." });
    }
}