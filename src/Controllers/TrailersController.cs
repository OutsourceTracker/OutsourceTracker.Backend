using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OutsourceTracker.Geolocation;
using OutsourceTracker.Models.Trailers;
using OutsourceTracker.Services.ModelService;
using System.Security.Claims;

namespace OutsourceTracker.Controllers;

[Route("[controller]")]
[ApiController]
[Authorize(Roles = "Trailers.Admin")]
public class TrailersController : ControllerBase
{
    private TrailerDataService Service { get; }

    public TrailersController(IServiceProvider service)
    {
        Service = service.GetRequiredService<TrailerDataService>();
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Trailer[]))]
    public IAsyncEnumerable<Trailer> Get()
    {
        var parameters = Request.Query.ToDictionary(
            q => q.Key,
            q => q.Value.Count > 1 ? (object)q.Value.ToArray() : q.Value[0]
        );
        var search = Service.Search(parameters, HttpContext.RequestAborted);
        return search;
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Trailer))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id)
    {
        Trailer? model = await Service.Get(id, HttpContext.RequestAborted);

        if (model == null)
        {
            return NotFound();
        }

        return Ok(model);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(Trailer))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Post()
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }


        Guid? id = await Service.Create(HttpContext.RequestAborted);

        if (!id.HasValue)
        {
            return BadRequest("Failed to create the trailer.");
        }

        return CreatedAtAction(nameof(Get), new { id = id.Value });
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        bool response = await Service.Delete(id, HttpContext.RequestAborted);

        if (!response)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Trailer))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Put(Guid id, [FromBody] IDictionary<string, object> request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        Trailer? model = await Service.Update(id, request, HttpContext.RequestAborted);

        if (model == null)
        {
            return NotFound();
        }

        return AcceptedAtAction(nameof(Get), new { id }, model);
    }

    [HttpPut("{id}/[action]")]
    [ProducesResponseType(StatusCodes.Status202Accepted, Type = typeof(DBNull))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(DBNull))]
    public async Task<IActionResult> Spot(Guid id, [FromBody] MapCoordinates coordinates)
    {
        Guid userId = Guid.Empty;

        if (!Guid.TryParse(User.FindFirstValue("sub"), out userId))
        {
            userId = Guid.Empty;
        }

        var update = new
        {
            Location = coordinates,
            LocatedBy = User.FindFirstValue("name") ?? "Unknown User",
            LocatedById = userId,
            LocatedDate = DateTimeOffset.UtcNow
        };

        Trailer? model = await Service.Update(id, update, HttpContext.RequestAborted);

        if (model != null)
        {
            return AcceptedAtAction(nameof(Get), new { id }, model);
        }

        return NotFound(id);
    }
}
