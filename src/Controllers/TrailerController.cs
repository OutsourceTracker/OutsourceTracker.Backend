using Microsoft.AspNetCore.Mvc;
using OutsourceTracker.Models.Trailers;
using OutsourceTracker.ModelService;
using OutsourceTracker.ModelService.Requests;
using OutsourceTracker.ModelService.Requests.Trailers;

namespace OutsourceTracker.Controllers;

[Route("[controller]")]
[ApiController]
public class TrailerController : ControllerBase
{
    private TrailerService Service { get; }

    public TrailerController(TrailerService service)
    {
        Service = service;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CommercialTrailer[]))]
    public IAsyncEnumerable<CommercialTrailer> Get([FromQuery] TrailerFindRequest? request = null)
    {
        var search = Service.Find(request, HttpContext.RequestAborted);
        return search;
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CommercialTrailer))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id)
    {
        CommercialTrailer? model = await Service.Get(id, HttpContext.RequestAborted);

        if (model == null)
        {
            return NotFound();
        }

        return Ok(model);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(CommercialTrailer))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Post([FromBody] TrailerCreateRequest? request = null)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }


        Guid? id = await Service.Create(request, HttpContext.RequestAborted);

        if (!id.HasValue)
        {
            return BadRequest("Failed to create the trailer.");
        }

        return CreatedAtAction(nameof(Get), new { id.Value });
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, DeleteRequest? request = null)
    {
        bool response = await Service.Delete(id, request, HttpContext.RequestAborted);

        if (!response)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CommercialTrailer))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Put(Guid id, [FromBody] TrailerUpdateRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        CommercialTrailer? model = await Service.Update(id, request, HttpContext.RequestAborted);

        if (model == null)
        {
            return NotFound();
        }

        return AcceptedAtAction(nameof(Get), new { id }, model);
    }

    [HttpGet("{id}/spot")]
    [ProducesResponseType(StatusCodes.Status202Accepted, Type = typeof(CommercialTrailer))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Guid))]
    public async Task<IActionResult> Spot(Guid id, [FromQuery] double lat, [FromQuery] double lon, [FromQuery] double acc, [FromQuery] string name)
    {
        TrailerUpdateRequest request = new()
        {
            Latitude = lat,
            Longitude = lon,
            SpottedBy = name,
            Accuracy = acc
        };

        CommercialTrailer? model = await Service.Update(id, request, HttpContext.RequestAborted);

        if (model != null)
        {
            return AcceptedAtAction(nameof(Get), new { id }, model);
        }

        return NotFound(id);
    }
}
