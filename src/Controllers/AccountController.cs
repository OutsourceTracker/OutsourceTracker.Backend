using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OutsourceTracker.BusinessUnit.Accounts;
using OutsourceTracker.Services.DataModels;

namespace OutsourceTracker.Controllers;

[Authorize]
public class AccountController : BaseApiController
{
    public IDataModelService<Guid, AccountDbModel> Accounts { get; }

    public AccountController(IServiceProvider services) : base(services)
    {
        Accounts = services.GetRequiredService<AccountService>();
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        ModelResult result = await Accounts.Get(id, HttpContext.RequestAborted);

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

        AccountDbModel? data = result.Data as AccountDbModel;

        if (data == null)
        {
            return NotFound();
        }

        return Ok(new OrganizationalAccount()
        {
            Id = data.Id,
            ShortCode = data.ShortCode,
            Name = data.Name,
            OUID = data.OUID,
            CreatedOn = data.CreatedOn
        });
    }

    [HttpGet]
    public async IAsyncEnumerable<OrganizationalAccount> Get()
    {
        if (!ModelState.IsValid)
        {
            yield break;
        }

        var parameters = Request.Query.ToDictionary(
            q => q.Key,
            q => q.Value.Count > 1 ? (object)q.Value.ToArray() : q.Value[0]
        );

        ModelResult result = await Accounts.Search(parameters, HttpContext.RequestAborted);

        if (result.Errors != null)
        {
            foreach (var k in result.Errors)
            {
                ModelState.AddModelError(k.Key, k.Value.ToString() ?? string.Empty);
            }
        }

        IAsyncEnumerable<AccountDbModel>? data = result.Data as IAsyncEnumerable<AccountDbModel>;

        if (!result.Success || data == null)
        {
            yield break;
        }

        await foreach (var item in data.WithCancellation(HttpContext.RequestAborted))
        {
            yield return new OrganizationalAccount()
            {
                Id = item.Id,
                ShortCode = item.ShortCode,
                Name = item.Name,
                OUID = item.OUID,
                CreatedOn = item.CreatedOn
            };
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AccountCreateModel parameters)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        ModelResult result = await Accounts.Create(parameters, HttpContext.RequestAborted);

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

        AccountDbModel? data = result.Data as AccountDbModel;

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
    public async Task<IActionResult> Update(Guid id, [FromBody] AccountCreateModel parameters)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        ModelResult result = await Accounts.Update(id, parameters, HttpContext.RequestAborted);

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

        AccountDbModel? data = result.Data as AccountDbModel;

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

        ModelResult result = await Accounts.Delete(id, HttpContext.RequestAborted);

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

        AccountDbModel? data = result.Data as AccountDbModel;

        if (data != null)
        {
            return NoContent();
        }
        else
        {
            return BadRequest(ModelState);
        }
    }

    public record AccountCreateModel(string ShortCode, string Name, Guid OUID);
}
