using App.DTOs;
using App.Enums;
using App.Models;
using App.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace App.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScheduleController : ControllerBase
{
    private readonly ScheduleService _scheduleService;
    private readonly UserManager<UserModel> _userManager;

    public ScheduleController(ScheduleService scheduleService, UserManager<UserModel> userManager)
    {
        _userManager = userManager;
        _scheduleService = scheduleService;
    }

    // Endpoint för att hämta en användares schema.
    [HttpGet("schedule"), Authorize]
    public async Task<ActionResult<ScheduleResponse>> GetSchedule()
    {
        string? userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            return Unauthorized(new { Message = "User ID not found in claims." });
        }

        UserModel? user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Unauthorized(new { Message = "User not found." });
        }

        try {
            ScheduleResponse schedule = await _scheduleService.GetScheduleByUserID(user.Id);
            return Ok(schedule);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while retrieving the schedule: {ex.Message}");
            return StatusCode(500, new { Message = "An error occurred while processing your request." });
        }
    }

    // Endpoint för att lägga till en ny schedule entry. Om en entry för samma anime redan finns för användaren, returneras ett conflict response.
    [HttpPost("entry"), Authorize]
    public async Task<ActionResult> EnterScheduleEntry(ScheduleRequest request)
    {
        string? userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            return Unauthorized(new { Message = "User ID not found in claims." });
        }

        UserModel? user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Unauthorized(new { Message = "User not found." });
        }

        try
        {
            await _scheduleService.AddScheduleEntry(user.Id, request);
            return Ok(new { Message = "Schedule entry added successfully." });
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("Schedule entry already exists"))
            {
                return Conflict(new { Message = ex.Message });
            }
            Console.WriteLine($"An error occurred while adding the schedule entry: {ex.Message}");
            return StatusCode(500, new { Message = "An error occurred while processing your request." });
        }
    }
}