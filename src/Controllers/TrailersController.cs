using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OutsourceTracker.Equipment;
using OutsourceTracker.Equipment.Trailers;
using OutsourceTracker.Geolocation;
using OutsourceTracker.Services.DataModels;
using OutsourceTracker.Services.ModelService;
using OutsourceTracker.Tools;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace OutsourceTracker.Controllers;

[ApiController]
[Authorize]
[Route("[controller]")]
public class TrailersController : ControllerBase
{
    private TrailerService Service { get; }
    private readonly ILogger<TrailersController> _logger;

    public TrailersController(IServiceProvider service, ILogger<TrailersController> logger)
    {
        Service = service.GetRequiredService<TrailerService>();
        _logger = logger;
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
    public async Task<IActionResult> Post([FromBody] TrailerCreateRequest model)
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
            return CreatedAtAction(nameof(Get), new { id = created.Id }, result.Data);

        }
        else
        {
            return BadRequest(ModelState);
        }
    }

    [HttpPost("bulk")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BulkCreateResult<TrailerModel>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkPost([FromBody] IEnumerable<TrailerCreateRequest> models)
    {
        if (models == null)
        {
            return BadRequest("No trailer data provided.");
        }

        ModelResult result = await Service.BulkCreate(models, HttpContext.RequestAborted);

        if (result.Errors != null)
        {
            foreach (var k in result.Errors)
            {
                ModelState.AddModelError(k.Key, k.Value.ToString() ?? string.Empty);
            }
        }

        if (result.Data is BulkCreateResult<TrailerModel> bulkResult)
        {
            // Return 200 even on partial success so the client can see what succeeded/failed.
            return Ok(bulkResult);
        }

        if (!result.Success)
        {
            return BadRequest(ModelState);
        }

        return Ok(new BulkCreateResult<TrailerModel>());
    }

    [HttpPost("bulk-delete")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BulkDeleteResult))]
    public async Task<IActionResult> BulkDelete([FromBody] Guid[] ids)
    {
        if (ids == null || ids.Length == 0)
        {
            return BadRequest("No trailer IDs provided.");
        }

        ModelResult result = await Service.BulkDelete(ids, HttpContext.RequestAborted);

        if (result.Data is BulkDeleteResult deleteResult)
        {
            return Ok(deleteResult);
        }

        return result.Success ? Ok(new BulkDeleteResult()) : BadRequest(ModelState);
    }

    [HttpPost("bulk-update")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BulkUpdateResult<TrailerModel>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkUpdate([FromBody] BulkTrailerUpdateRequest request)
    {
        if (request?.Ids == null || request.Ids.Length == 0)
        {
            return BadRequest("No trailer IDs provided.");
        }

        if (request.Changes == null || request.Changes.Count == 0)
        {
            return BadRequest("No changes provided.");
        }

        ModelResult result = await Service.BulkUpdate(request.Ids, request.Changes, HttpContext.RequestAborted);

        if (result.Data is BulkUpdateResult<TrailerModel> updateResult)
        {
            return Ok(updateResult);
        }

        return result.Success ? Ok(new BulkUpdateResult<TrailerModel>()) : BadRequest(ModelState);
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

    [HttpPut("[action]")]
    [ProducesResponseType(StatusCodes.Status202Accepted, Type = typeof(DBNull))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(DBNull))]
    public async Task<IActionResult> Spot(EquipmentLocationUpdateRequest<Guid> request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (request.Ids == null || request.Ids.Length == 0)
        {
            return BadRequest("No IDs provided.");
        }

        if (request.Location == Vector2.Zero)
        {
            return BadRequest("Invalid location provided.");
        }

        Guid userId = Guid.Empty;

        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId))
        {
            userId = Guid.Empty;
        }

        var update = ModelUpdater<TrailerDbModel>.Update(ControllerContext.HttpContext.RequestServices)
            .Set(x => x.Location, request.Location)
            .Set(x => x.LocationAccuracy, request.Accuracy)
            .Set(x => x.LocatedByName, User.Identity?.Name ?? "Unknown User")
            .Set(x => x.LocatedById, userId)
            .Set(x => x.LocatedDate, DateTimeOffset.UtcNow)
            .Build();


        List<Guid> success = [];
        Dictionary<Guid, string> failed = [];
        for (int i = 0; i < request.Ids.Length; i++)
        {
            ModelResult result = await Service.Update(request.Ids[i], update, HttpContext.RequestAborted);

            if (result.Success)
            {
                success.Add(request.Ids[i]);

            }
            else
            {
                if (result.Errors != null)
                {
                    string errorMessage = string.Join("; ", result.Errors.Select(e => $"{e.Key}: {e.Value}"));
                    failed.Add(request.Ids[i], errorMessage);
                }
                else
                {
                    failed.Add(request.Ids[i], "Unknown error");
                }
            }
        }

        return Ok(new EquipmentLocationUpdateResponse<Guid>
        {
            Success = failed.Count == 0,
            SuccessfulTrailers = success.ToArray(),
            FailedTrailers = failed
        });
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
}
