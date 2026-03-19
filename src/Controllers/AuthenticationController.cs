using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OutsourceTracker.Authentication;
using OutsourceTracker.Services;
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
        var htmlContent = $@"
    <h2>Welcome to OutsourceTracker, {user.FullName}!</h2>
    <p>Please confirm your email by clicking this link:</p>
    <p><a href='{callback}'>Confirm my email address</a></p>
    <p>If you didn't sign up, ignore this email.</p>
    <p>— OutsourceTracker Team</p>";

        try
        {
            await _email.SendEmailAsync(user.Email, "OutsourceTracker - Confirm Your Email", htmlContent);
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
        var user = await _users.FindByEmailAsync(dto.Email);

        if (user == null) return Unauthorized();

        var result = await _signIn.CheckPasswordSignInAsync(user, dto.Password, false);

        if (!result.Succeeded) return Unauthorized();

        var token = await _token.GenerateTokenAsync(user);
        return Ok(new { token });
    }

    public record LoginDto(string Email, string Password);

    public record RegisterDto(string Email, string Password, string FirstName, string LastName, string AlphaCode, string WorkdayId);
}
