using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OutsourceTracker.Equipment;
using OutsourceTracker.Equipment.Trailers;
using OutsourceTracker.Geolocation;
using OutsourceTracker.Models.Trailers;
using OutsourceTracker.Services.ModelService;
using System.Security.Claims;

namespace OutsourceTracker.Controllers;

[ApiController]
[Authorize(Roles = "Trailers.Read")]
[Route("[controller]")]
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
    [Authorize(Roles = "Trailers.Write")]
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

        return CreatedAtAction(nameof(Get), id.Value);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Trailers.Write")]
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
    [Authorize(Roles = "Trailers.Write")]
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
    [Authorize(Roles = "Trailers.Write,Trailers.UpdateLocation")]
    [ProducesResponseType(StatusCodes.Status202Accepted, Type = typeof(DBNull))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(DBNull))]
    public async Task<IActionResult> Spot(Guid id, [FromBody] Vector2 coordinates, [FromQuery] double? acc = 0.00)
    {
        Guid userId = Guid.Empty;

        if (!Guid.TryParse(User.FindFirstValue("sub"), out userId))
        {
            userId = Guid.Empty;
        }

        var update = new
        {
            Location = coordinates,
            LocationAccuracy = acc.GetValueOrDefault(),
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

    [AllowAnonymous]
    [HttpGet("[action]")]
    public async IAsyncEnumerable<object> GetExcelUpdate([FromQuery]string authKey, [FromQuery] Guid? accountId = null)
    {
        if (authKey != "JBHUNT_WINCO")
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            yield break;
        }

        var trailerQuery = Service.Search(null, HttpContext.RequestAborted);

        var e = trailerQuery.GetAsyncEnumerator(HttpContext.RequestAborted);
        
        while (await e.MoveNextAsync())
        {
            string mapsLink = e.Current.Location.HasValue
                ? $"https://www.google.com/maps/search/?api=1&query={e.Current.Location.Value.X},{e.Current.Location.Value.Y}"
                : "No Location";

            yield return new
            {
                Id = e.Current.Id,
                Prefix = e.Current.Prefix,
                Name = e.Current.Name,
                State = Enum.GetName(typeof(EquipmentState), e.Current.State),
                Type = Enum.GetName(typeof(TrailerType), e.Current.Type),
                YardName = e.Current.ZoneName,
                AttachedTo = string.Empty,
                Location = mapsLink,
                LocatedBy = e.Current.LocatedBy,
                LocatedDate = e.Current.LocatedDate.HasValue ? e.Current.LocatedDate.Value.ToLocalTime().ToString("MM-dd-yyyy HH:mm:ss") : null
            };
        }
    }
}
