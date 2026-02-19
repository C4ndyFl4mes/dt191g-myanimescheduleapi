using App.DTOs;
using App.Extensions;
using App.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScheduleController(ScheduleService _scheduleService) : ControllerBase
{
    // Endpoint för GET: den hämtar en användares schedule.
    [HttpGet("schedule"), Authorize]
    public async Task<ActionResult<ScheduleResponse>> GetSchedule()
    {
        int userID = User.GetUserID();

        ScheduleResponse schedule = await _scheduleService.GetScheduleByUserID(userID);
        return Ok(schedule);
    }

    // Endpoint för POST: den skapar en schedule entry för en användare.
    [HttpPost("entry"), Authorize]
    public async Task<ActionResult> PostScheduleEntry(ScheduleRequest request)
    {
        int userID = User.GetUserID();

        await _scheduleService.AddScheduleEntry(userID, request);
        return CreatedAtAction(nameof(PostScheduleEntry), new { Message = "Schedule entry added successfully." });
    }

    // Endpoint för PUT: den ändrar en schedule entry för en användare.
    [HttpPut("entry"), Authorize]
    public async Task<ActionResult> UpdateScheduleEntry(ScheduleUpdateRequest request)
    {
        int userID = User.GetUserID();

        await _scheduleService.UpdateScheduleEntry(userID, request);
        return Ok(new { Message = "Schedule entry update successfully." });
    }

    // Endpoint för DELETE: den raderar en schedule entry för en användare.
    [HttpDelete("entry"), Authorize]
    public async Task<ActionResult> DeleteScheduleEntry(int indexedAnimeId)
    {
        int userID = User.GetUserID();

        await _scheduleService.DeleteScheduleEntry(userID, indexedAnimeId);
        return NoContent();
    }
}