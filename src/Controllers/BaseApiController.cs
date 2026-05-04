using Microsoft.AspNetCore.Mvc;

namespace OutsourceTracker.Controllers;

[ApiController]
[Route("[controller]")]
public abstract class BaseApiController : ControllerBase
{
    protected ILogger Logger { get; }

    protected BaseApiController(IServiceProvider services)
    {
        ILoggerFactory logFactory = services.GetRequiredService<ILoggerFactory>();
        Logger = logFactory.CreateLogger(GetType());
    }
}
