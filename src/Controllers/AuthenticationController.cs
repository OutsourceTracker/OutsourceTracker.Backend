using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OutsourceTracker.Authentication;
using OutsourceTracker.Services;
using System.Security.Claims;
using System.Web;

namespace OutsourceTracker.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthenticationController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly JwtTokenService _token;
    private readonly EmailService _email;

    public AuthenticationController(IServiceProvider services)
    {
        _users = services.GetRequiredService<UserManager<ApplicationUser>>();
        _signIn = services.GetRequiredService<SignInManager<ApplicationUser>>();
        _token = services.GetRequiredService<JwtTokenService>();
        _email = services.GetRequiredService<EmailService>();
    }

    [HttpPost("[action]")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            AlphaCode = dto.AlphaCode,
            WorkdayId = dto.WorkdayId,
            FullName = $"{dto.FirstName} {dto.LastName}"
        };

        var createResult = await _users.CreateAsync(user, dto.Password);

        if (!createResult.Succeeded)
        {
            foreach (var error in createResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return BadRequest(ModelState);
        }

        var token = await _users.GenerateEmailConfirmationTokenAsync(user);
        var backendUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}";

        var callback = $"{backendUrl}/Authentication/ConfirmEmail?userId={user.Id}&token={HttpUtility.UrlEncode(token)}";

        try
        {
            await _email.SendTemplateEmailAsync(user.Email, "d-a4d18fe8a97f4d5d9e73062c89f9d7bb", new Dictionary<string, string>
            {
                ["email"] = user.Email,
                ["firstName"] = user.FirstName,
                ["lastName"] = user.LastName,
                ["fullName"] = user.FullName,
                ["alphaCode"] = user.AlphaCode,
                ["workdayId"] = user.WorkdayId,
                ["callback_url"] = callback
            });
        }
        catch (Exception)
        {
            return Created();
        }

        return Created();
    }

    [HttpGet("[action]")]
    public async Task<IActionResult> ConfirmEmail([FromQuery] Guid userId, [FromQuery] string token)
    {
        var user = await _users.FindByIdAsync(userId.ToString());

        if (user == null) return BadRequest("Invalid user");

        var result = await _users.ConfirmEmailAsync(user, token);

        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok("Email confirmed successfully. You can now log in.");
    }

    [HttpPost("[action]")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        
        var user = await _users.FindByEmailAsync(dto.Email);

        if (user == null)
            return Unauthorized("Invalid email or password");

        var result = await _signIn.CheckPasswordSignInAsync(user, dto.Password, true);

        if (!result.Succeeded)
            return Unauthorized("Invalid email or password");

        var token = await _token.GenerateTokenAsync(user);
        return Ok(new { token });
    }

    [HttpGet("[action]")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        if (User.Identity?.IsAuthenticated == false)
        {
            return Unauthorized("You are not logged in");
        }

        return Ok(new
        {
            Id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            FirstName = User.FindFirst(ClaimTypes.GivenName)?.Value,
            LastName = User.FindFirst(ClaimTypes.Surname)?.Value,
            Email = User.FindFirst(ClaimTypes.Email)?.Value,
            Roles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToArray()
        });
    }

    public record LoginDto(string Email, string Password);

    public record RegisterDto(string Email, string Password, string FirstName, string LastName, string AlphaCode, string WorkdayId);
}
