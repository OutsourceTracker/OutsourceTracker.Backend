using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OutsourceTracker.BusinessUnit.Divisions;
using OutsourceTracker.Services.DataModels;

namespace OutsourceTracker.Controllers;

[Authorize]
public class OUController : BaseApiController
{
    public IDataModelService<Guid, OrganizationalUnitDbModel> OU { get; }

    public OUController(IServiceProvider services) : base(services)
    {
        OU = services.GetRequiredService<OrganizationalUnitService>();
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        ModelResult result = await OU.Get(id, HttpContext.RequestAborted);

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

        OrganizationalUnitDbModel? data = result.Data as OrganizationalUnitDbModel;

        if (data == null)
        {
            return NotFound();
        }

        return Ok(new OrganizationalUnit()
        {
            Id = data.Id,
            ShortCode = data.ShortCode,
            Name = data.Name,
            TotalAccounts = data.TotalAccounts,
            Description = data.Description,
            CreatedOn = data.CreatedOn
        });
    }

    [HttpGet]
    public async IAsyncEnumerable<OrganizationalUnit> Get()
    {
        if (!ModelState.IsValid)
        {
            yield break;
        }

        var parameters = Request.Query.ToDictionary(
            q => q.Key,
            q => q.Value.Count > 1 ? (object)q.Value.ToArray() : q.Value[0]
        );

        ModelResult result = await OU.Search(parameters, HttpContext.RequestAborted);

        if (result.Errors != null)
        {
            foreach (var k in result.Errors)
            {
                ModelState.AddModelError(k.Key, k.Value.ToString() ?? string.Empty);
            }
        }

        IAsyncEnumerable<OrganizationalUnitDbModel>? data = result.Data as IAsyncEnumerable<OrganizationalUnitDbModel>;

        if (!result.Success || data == null)
        {
            yield break;
        }

        await foreach (var item in data.WithCancellation(HttpContext.RequestAborted))
        {
            yield return new OrganizationalUnit()
            {
                Id = item.Id,
                ShortCode = item.ShortCode,
                Name = item.Name,
                TotalAccounts = item.TotalAccounts,
                Description = item.Description,
                CreatedOn = item.CreatedOn
            };
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody]OUCreateModel parameters)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        ModelResult result = await OU.Create(parameters, HttpContext.RequestAborted);

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

        OrganizationalUnitDbModel? data = result.Data as OrganizationalUnitDbModel;

        if (data != null)
        {
            return CreatedAtAction(nameof(Get), new { id = data.Id });
        }
        else
        {
            return BadRequest(ModelState);
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody]OUCreateModel parameters)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        ModelResult result = await OU.Update(id, parameters, HttpContext.RequestAborted);

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

        OrganizationalUnitDbModel? data = result.Data as OrganizationalUnitDbModel;

        if (data != null)
        {
            return AcceptedAtAction(nameof(Get), new { id = data.Id });
        }
        else
        {
            return BadRequest(ModelState);
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        ModelResult result = await OU.Delete(id, HttpContext.RequestAborted);

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

        OrganizationalUnitDbModel? data = result.Data as OrganizationalUnitDbModel;

        if (data != null)
        {
            return NoContent();
        }
        else
        {
            return BadRequest(ModelState);
        }
    }

    public record OUCreateModel(string ShortCode, string Name, string Description);
}
