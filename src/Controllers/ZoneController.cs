using Microsoft.AspNetCore.Mvc;
using OutsourceTracker.Data;
using OutsourceTracker.Geolocation;
using OutsourceTracker.Models.Zones;

namespace OutsourceTracker.Controllers;

[Route("[controller]")]
[ApiController]
public class ZoneController : ControllerBase
{
    private AppDataContext Context { get; init; }

    public ZoneController(AppDataContext context)
    {
        Context = context;
    }

    [HttpGet]
    public async IAsyncEnumerable<Zone> Get()
    {
        var zones = Context.Zones.AsAsyncEnumerable();

        await foreach (var zone in zones)
        {
            yield return zone;
        }
    }


    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var zone = await Context.Zones.FindAsync([id], HttpContext.RequestAborted);
        
        if (zone is null)
        {
            return NotFound();
        }

        return Ok(zone);
    }

    [HttpPost("{zoneName}")]
    public async Task<IActionResult> Post(string zoneName, [FromBody] ICollection<Vector2> points)
    {
        if (string.IsNullOrWhiteSpace(zoneName))
        {
            return BadRequest("Zone name cannot be empty.");
        }

        var zone = new Zone
        {
            Id = Guid.CreateVersion7(DateTimeOffset.UtcNow),
            Name = zoneName,
            Boundry = new Polygon(points)
        };

        await Context.Zones.AddAsync(zone, HttpContext.RequestAborted);
        await Context.SaveChangesAsync(HttpContext.RequestAborted);
        return CreatedAtAction(nameof(Get), new { id = zone.Id });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var zone = await Context.Zones.FindAsync([id], HttpContext.RequestAborted);
        
        if (zone is null)
        {
            return NotFound();
        }

        Context.Zones.Remove(zone);
        await Context.SaveChangesAsync(HttpContext.RequestAborted);
        return NoContent();
    }
}
