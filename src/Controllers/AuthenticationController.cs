using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OutsourceTracker.Authentication;
using OutsourceTracker.Services;
using System.Security.Claims;
using System.Text.Json;
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
    public async Task<IActionResult> Register([FromBody] RegisterModel dto)
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
                ModelState.AddModelError(error.Code, error.Description);
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
    public async Task<IActionResult> Login([FromBody] LoginModel dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        
        var user = await _users.FindByEmailAsync(dto.Email);

        if (user == null)
            return Unauthorized("Invalid email or password");

        var result = await _signIn.CheckPasswordSignInAsync(user, dto.Password, true);

        if (!result.Succeeded)
            return Unauthorized("Invalid email or password");

        var token = await _token.GenerateTokenAsync(user, dto.RememberMe);
        return Ok(token);
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

    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var id))
        {
            return Unauthorized();
        }

        var user = await _users.FindByIdAsync(id.ToString());
        if (user == null)
        {
            return NotFound();
        }

        return Ok(new
        {
            user.Id,
            user.FirstName,
            user.LastName,
            user.FullName,
            user.AlphaCode,
            user.WorkdayId,
            user.Email
        });
    }

    public record UpdateProfileRequest(string? FirstName, string? LastName, string? AlphaCode, string? WorkdayId);

    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var id))
        {
            return Unauthorized();
        }

        var user = await _users.FindByIdAsync(id.ToString());
        if (user == null)
        {
            return NotFound();
        }

        // Apply updates
        if (!string.IsNullOrWhiteSpace(dto.FirstName))
            user.FirstName = dto.FirstName;

        if (!string.IsNullOrWhiteSpace(dto.LastName))
            user.LastName = dto.LastName;

        user.FullName = $"{user.FirstName} {user.LastName}".Trim();

        if (dto.AlphaCode != null)
            user.AlphaCode = dto.AlphaCode;

        if (dto.WorkdayId != null)
            user.WorkdayId = dto.WorkdayId;

        var result = await _users.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(error.Code, error.Description);
            }
            return BadRequest(ModelState);
        }

        return Ok(new
        {
            user.Id,
            user.FirstName,
            user.LastName,
            user.FullName,
            user.AlphaCode,
            user.WorkdayId,
            user.Email
        });
    }

    // ==================== PASSKEY SUPPORT (Identity + Fido2NetLib) ====================

    private PasskeyService PasskeyService => HttpContext.RequestServices.GetRequiredService<PasskeyService>();

    [HttpGet("passkey/registration-options")]
    [Authorize]
    public async Task<IActionResult> GetPasskeyRegistrationOptions([FromQuery] string? displayName)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var id))
            return Unauthorized();

        var user = await _users.FindByIdAsync(id.ToString());
        if (user == null) return NotFound();

        var options = await PasskeyService.GetRegistrationOptionsAsync(user, displayName);
        return Ok(options);
    }

    // The frontend (passkey.js) sends the full WebAuthn response. Accept it as Fido2's raw type.
    [HttpPost("passkey/complete-registration")]
    [Authorize]
    public async Task<IActionResult> CompletePasskeyRegistration([FromBody] AuthenticatorAttestationRawResponse attestationResponse, [FromQuery] string? name)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var id))
            return Unauthorized();

        var user = await _users.FindByIdAsync(id.ToString());
        if (user == null) return NotFound();

        // Defensive: Fido2 5.x models require ClientExtensionResults
        if (attestationResponse.ClientExtensionResults == null)
            attestationResponse.ClientExtensionResults = new AuthenticationExtensionsClientOutputs();

        var result = await PasskeyService.CompleteRegistrationAsync(user, attestationResponse, name);

        return result.Succeeded
            ? Ok(new { Message = "Passkey registered successfully" })
            : BadRequest(result.Errors);
    }

    [HttpGet("passkey/assertion-options")]
    public async Task<IActionResult> GetPasskeyAssertionOptions()
    {
        var options = await PasskeyService.GetAssertionOptionsAsync();
        return Ok(options);
    }

    // Accept raw assertion response directly from the browser.
    [HttpPost("passkey/complete-assertion")]
    public async Task<IActionResult> CompletePasskeyAssertion([FromBody] AuthenticatorAssertionRawResponse assertionResponse, [FromQuery] bool? rememberMe)
    {
        // Defensive: Fido2 5.x models require ClientExtensionResults
        if (assertionResponse.ClientExtensionResults == null)
            assertionResponse.ClientExtensionResults = new AuthenticationExtensionsClientOutputs();

        var (success, user) = await PasskeyService.CompleteAssertionAsync(assertionResponse);
        if (!success || user == null)
            return Unauthorized("Passkey authentication failed");

        var token = await _token.GenerateTokenAsync(user, rememberMe ?? false);
        return Ok(token);
    }

    [HttpGet("passkeys")]
    [Authorize]
    public async Task<IActionResult> GetUserPasskeys()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var id)) return Unauthorized();

        var passkeys = await PasskeyService.GetUserPasskeysAsync(id);
        return Ok(passkeys);
    }

    [HttpDelete("passkeys/{credentialId}")]
    [Authorize]
    public async Task<IActionResult> DeletePasskey(string credentialId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var id)) return Unauthorized();

        var success = await PasskeyService.DeletePasskeyAsync(id, credentialId);
        return success ? Ok(new { Message = "Passkey deleted successfully" }) : NotFound();
    }

    private static byte[] Base64UrlDecode(string input)
    {
        if (string.IsNullOrEmpty(input)) return Array.Empty<byte>();
        var base64 = input.Replace('-', '+').Replace('_', '/');
        switch (base64.Length % 4) { case 2: base64 += "=="; break; case 3: base64 += "="; break; }
        return Convert.FromBase64String(base64);
    }
}
