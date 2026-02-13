using App.Data;
using Microsoft.AspNetCore.Mvc;

namespace App.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnimeController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AnimeController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("indexed")]
    public async Task<IActionResult> GetIndexedAnimes()
    {
        var indexedAnimes = await _context.IndexedAnimes.AsAsyncEnumerable().ToListAsync();
        return Ok(indexedAnimes);
    }
}