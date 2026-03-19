using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OutsourceTracker.Geolocation;
using OutsourceTracker.Models.Zones;
using OutsourceTracker.Services.ModelService;

namespace OutsourceTracker.Controllers;

[ApiController]
[Authorize(Roles = "Zones.Read")]
[Route("[controller]")]
public class ZoneController : ControllerBase
{
    private ZoneDataService Context { get; }

    public ZoneController(IServiceProvider service)
    {
        Context = service.GetRequiredService<ZoneDataService>();
    }

    [HttpGet]
    public async IAsyncEnumerable<Zone> Get()
    {
        var zones = Context.Search(cancellationToken: HttpContext.RequestAborted);

        await foreach (var zone in zones)
        {
            yield return zone;
        }
    }

    [HttpGet("[action]")]
    [AllowAnonymous]
    public async Task<IActionResult> IsInZone([FromQuery] double x, [FromQuery] double y)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        Vector2 coords = new Vector2(x, y);
        var zones = Context.Search(cancellationToken: HttpContext.RequestAborted);
        
        await foreach (var zone in zones)
        {
            if (zone.Boundry.Contains(coords))
            {
                return Ok(new { zone.Id, zone.ShortCode, zone.FullName });
            }
        }

        return NotFound();
    }


    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        try
        {
            var zone = await Context.Get(id, HttpContext.RequestAborted);

            if (zone is null)
            {
                return NotFound();
            }

            return Ok(zone);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(id);
        }
        catch (ArgumentNullException)
        {
            return BadRequest(nameof(id));
        }
    }

    [HttpPost("{zoneId}")]
    //[Authorize(Roles = "Zones.Write")]
    [AllowAnonymous]
    public async Task<IActionResult> Post(string zoneId, [FromBody] ZoneBuilder model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (model.BoundryPoints.Count < 4 || model.BoundryPoints.Distinct().Count() < 3)
        {
            return BadRequest("Need more points");
        }

        try
        {
            Zone zone = new Zone()
            {
                ShortCode = zoneId,
                FullName = model.FullName,
                Boundry = new Polygon(model.BoundryPoints),
                EntryPoints = model.EntryPoints ?? new List<Vector2>(),
                ExitPoints = model.ExitPoints ?? new List<Vector2>(),
                DockPoints = model.DockPouints ?? new List<Vector2>()
            };

            Zone? created = await Context.Create(zone, HttpContext.RequestAborted);
            return CreatedAtAction(nameof(Get), new { id = zone.Id }, created);
        }
        catch (ArgumentNullException)
        {
            return BadRequest();
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Zones.Write")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            int deleted = await Context.Delete(id, HttpContext.RequestAborted);

            if (deleted > 0)
            {
                return Ok(id);
            }

            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(id);
        }
    }

    public class ZoneBuilder
    {
        public string FullName { get; set; }

        public ICollection<Vector2> BoundryPoints { get; set; }

        public ICollection<Vector2>? EntryPoints { get; set; }

        public ICollection<Vector2>? ExitPoints { get; set; }

        public ICollection<Vector2>? DockPouints { get; set; }
    }
}
