using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OutsourceTracker.Geolocation;
using OutsourceTracker.Models.Zones;
using OutsourceTracker.Services.ModelService;

namespace OutsourceTracker.Controllers;

[ApiController]
[Authorize]
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
        var zone = await Context.FindZoneForLocationAsync(coords, HttpContext.RequestAborted);

        if (zone != null)
        {
            return Ok(new { zone.Id, zone.ShortCode, zone.FullName });
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

    /// <summary>
    /// Standard create for zone management (short code + name only).
    /// Geometry/boundary can be populated via the special /Zone/{zoneId} endpoint or other tools.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ZoneCreateModel model)
    {
        if (!ModelState.IsValid || model is null)
        {
            return BadRequest(ModelState);
        }

        if (string.IsNullOrWhiteSpace(model.ShortCode) || string.IsNullOrWhiteSpace(model.FullName))
        {
            return BadRequest("ShortCode and FullName are required.");
        }

        try
        {
            var zone = new Zone()
            {
                ShortCode = model.ShortCode,
                FullName = model.FullName
            };

            Zone? created = await Context.Create(zone, HttpContext.RequestAborted);
            // Return 201 with no body (consistent with Account/OU creates); client will refresh list
            return CreatedAtAction(nameof(Get), new { id = created?.Id ?? zone.Id });
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

    /// <summary>
    /// Standard update for zone management (ShortCode / FullName). Uses the same DTO as Create.
    /// Geometry updates continue to use the special POST {shortCode} endpoint.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ZoneCreateModel model)
    {
        if (!ModelState.IsValid || model is null)
        {
            return BadRequest(ModelState);
        }

        try
        {
            Zone updated = await Context.Update(id, model, HttpContext.RequestAborted);
            return Ok(updated);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(id);
        }
        catch (ArgumentNullException)
        {
            return BadRequest(nameof(id));
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }

    [HttpPost("{zoneId}")]
    [AllowAnonymous]
    public async Task<IActionResult> Post(string zoneId, [FromBody] ZoneBuilder model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (model.BoundryPoints == null || model.BoundryPoints.Count < 4 || model.BoundryPoints.Distinct().Count() < 3)
        {
            return BadRequest("Need at least 3 distinct boundary points (4 recommended to close the polygon).");
        }

        try
        {
            var normalizedCode = zoneId?.ToUpperInvariant().Trim();
            if (string.IsNullOrWhiteSpace(normalizedCode))
                return BadRequest("zoneId (ShortCode) is required in the URL path.");

            // If a zone with this ShortCode already exists (e.g. created via the management UI),
            // update its geometry instead of creating a duplicate record.
            var existing = await Context.GetByShortCodeAsync(normalizedCode, HttpContext.RequestAborted);

            if (existing != null)
            {
                // Apply geometry changes using the dynamic update pipeline (respects converters)
                var geometryChanges = new
                {
                    FullName = model.FullName ?? existing.FullName,
                    Boundry = new Polygon(model.BoundryPoints),
                    EntryPoints = model.EntryPoints ?? new List<Vector2>(),
                    ExitPoints = model.ExitPoints ?? new List<Vector2>(),
                    DockPoints = model.DockPoints ?? model.DockPouints ?? new List<Vector2>(),
                    TrailerPools = model.TrailerPools ?? new List<Vector2>()
                };

                var updated = await Context.Update(existing.Id, geometryChanges, HttpContext.RequestAborted);
                return Ok(updated);
            }

            // No existing zone — create a new one (original behavior)
            Zone zone = new Zone()
            {
                ShortCode = normalizedCode,
                FullName = model.FullName,
                Boundry = new Polygon(model.BoundryPoints),
                EntryPoints = model.EntryPoints ?? new List<Vector2>(),
                ExitPoints = model.ExitPoints ?? new List<Vector2>(),
                DockPoints = model.DockPoints ?? model.DockPouints ?? new List<Vector2>(),
                TrailerPools = model.TrailerPools ?? new List<Vector2>()
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

        // Original field name kept for backward compatibility with existing callers
        public ICollection<Vector2>? DockPouints { get; set; }

        // Preferred / corrected spelling (also supported)
        [System.Text.Json.Serialization.JsonPropertyName("dockPoints")]
        public ICollection<Vector2>? DockPoints { get; set; }

        public ICollection<Vector2>? TrailerPools { get; set; }
    }

    /// <summary>
    /// DTO for standard zone create/update operations from the management UI.
    /// </summary>
    public record ZoneCreateModel(string ShortCode, string FullName);
}
