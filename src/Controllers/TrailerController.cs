using Microsoft.AspNetCore.Mvc;
using OutsourceTracker.Geolocation;
using OutsourceTracker.Models.Trailers;
using OutsourceTracker.Services.ModelService;

namespace OutsourceTracker.Controllers;

[Route("[controller]")]
[ApiController]
public class TrailerController : ControllerBase
{
    private TrailerDataService Service { get; }

    public TrailerController(TrailerDataService service)
    {
        Service = service;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Trailer[]))]
    public IAsyncEnumerable<Trailer> Get([FromQuery] object? request = null)
    {
        var search = Service.Search(null, HttpContext.RequestAborted);
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

        return CreatedAtAction(nameof(Get), new { id.Value });
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
    public async Task<IActionResult> Put(Guid id, [FromBody] IDictionary<object, object> request)
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

    [HttpGet("{id}/spot")]
    [ProducesResponseType(StatusCodes.Status202Accepted, Type = typeof(Trailer))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Guid))]
    public async Task<IActionResult> Spot(Guid id, [FromQuery] double lat, [FromQuery] double lon, [FromQuery] double acc, [FromQuery] string name)
    {
        var update = new
        {
            Location = new MapCoordinates(lat, lon, acc),
            LocatedBy = name,
            LocatedAt = (DateTimeOffset)DateTime.Now
        };

        Trailer? model = await Service.Update(id, update, HttpContext.RequestAborted);

        if (model != null)
        {
            return AcceptedAtAction(nameof(Get), new { id }, model);
        }

        return NotFound(id);
    }
}
