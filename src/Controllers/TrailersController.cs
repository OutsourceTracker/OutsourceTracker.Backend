using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OutsourceTracker.Equipment;
using OutsourceTracker.Equipment.Trailers;
using OutsourceTracker.Geolocation;
using OutsourceTracker.Services.DataModels;
using System.Security.Claims;

namespace OutsourceTracker.Controllers;

[ApiController]
[Authorize]
[Route("[controller]")]
public class TrailersController : ControllerBase
{
    private TrailerService Service { get; }

    public TrailersController(IServiceProvider service)
    {
        Service = service.GetRequiredService<TrailerService>();
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TrailerModel[]))]
    public async IAsyncEnumerable<TrailerModel> Get()
    {
        var parameters = Request.Query.ToDictionary(
            q => q.Key,
            q => q.Value.Count > 1 ? (object)q.Value.ToArray() : q.Value[0]
        );
        ModelResult result = await Service.Search(parameters, HttpContext.RequestAborted);

        if (!result.Success)
        {
            yield break;
        }

        IAsyncEnumerable<TrailerModel> list = result.Data is IAsyncEnumerable<TrailerModel> trailers ? trailers : AsyncEnumerable.Empty<TrailerModel>();

        await foreach (var trailer in list.WithCancellation(HttpContext.RequestAborted))
        {
            yield return trailer;
        }
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TrailerModel))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id)
    {
        ModelResult result = await Service.Get(id, HttpContext.RequestAborted);

        if (result.Errors != null)
        {
            foreach (var k in result.Errors)
            {
                ModelState.AddModelError(k.Key, k.Value.ToString() ?? string.Empty);
            }
        }

        if (result.Success)
        {
            TrailerModel model = (TrailerModel)result.Data!;
            return Ok(model);
        }
        else
        {
            return Conflict(ModelState);
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(TrailerModel))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Post([FromBody] TrailerCreateModel model)
    {
        if (!ModelState.IsValid || model == null)
        {
            return BadRequest(ModelState);
        }

        ModelResult result = await Service.Create(model, HttpContext.RequestAborted);

        if (result.Errors != null)
        {
            foreach (var k in result.Errors)
            {
                ModelState.AddModelError(k.Key, k.Value.ToString() ?? string.Empty);
            }
        }

        if (result.Success)
        {
            TrailerModel created = (TrailerModel)result.Data!;
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);

        }
        else
        {
            return BadRequest(ModelState);
        }
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        ModelResult result = await Service.Delete(id, HttpContext.RequestAborted);

        if (result.Errors != null)
        {
            foreach (var k in result.Errors)
            {
                ModelState.AddModelError(k.Key, k.Value.ToString() ?? string.Empty);
            }
        }

        if (!result.Success)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TrailerModel))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Put(Guid id, [FromBody] IDictionary<string, object> request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        ModelResult result = await Service.Update(id, request, HttpContext.RequestAborted);

        if (result.Errors != null)
        {
            foreach (var k in result.Errors)
            {
                ModelState.AddModelError(k.Key, k.Value.ToString() ?? string.Empty);
            }
        }

        if (!result.Success)
        {
            return BadRequest(ModelState);
        }

        TrailerModel updated = (TrailerModel)result.Data!;
        return Ok(updated);
    }

    [HttpPut("{id}/[action]")]
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

        ModelResult result = await Service.Update(id, update, HttpContext.RequestAborted);

        if (result.Errors != null)
        {
            foreach (var k in result.Errors)
            {
                ModelState.AddModelError(k.Key, k.Value.ToString() ?? string.Empty);
            }
        }

        if (!result.Success)
        {
            return BadRequest(ModelState);
        }

        return AcceptedAtAction(nameof(Get), new { id }, result.Data);
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

        ModelResult result = await Service.Search((object?)null, HttpContext.RequestAborted);

        if (!result.Success)
        {
            yield break;
        }

        IAsyncEnumerable<TrailerModel> list = result.Data is IAsyncEnumerable<TrailerModel> trailers ? trailers : AsyncEnumerable.Empty<TrailerModel>();
        IAsyncEnumerator<TrailerModel> e = list.GetAsyncEnumerator(HttpContext.RequestAborted);
        
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
                LocatedBy = e.Current.LocatedByName,
                LocatedDate = e.Current.LocatedDate.HasValue ? e.Current.LocatedDate.Value.ToLocalTime().ToString("MM-dd-yyyy HH:mm:ss") : null
            };
        }
    }

    #region Controller Specific Classes

    public record TrailerCreateModel(string Prefix, string Name, TrailerType Type = TrailerType.Van);

    #endregion
}
