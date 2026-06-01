using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using OutsourceTracker.Authentication;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace OutsourceTracker.Services;

public class JwtTokenService
{
    private readonly IConfiguration _config;
    private readonly UserManager<ApplicationUser> _users;

    public JwtTokenService(IConfiguration config, UserManager<ApplicationUser> users)
    {
        _config = config.GetRequiredSection("Jwt");
        _users = users;
    }

    public async Task<string> GenerateTokenAsync(ApplicationUser user, bool rememberMe = false)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim(JwtRegisteredClaimNames.Name, user.FullName ?? user.UserName!),
            new Claim(JwtRegisteredClaimNames.FamilyName, user.LastName),
            new Claim(JwtRegisteredClaimNames.GivenName, user.FirstName),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.FullName ?? user.UserName!),
            new Claim(ClaimTypes.Email, user.Email!),
            new Claim(ClaimTypes.GivenName, user.FirstName),
            new Claim(ClaimTypes.Surname, user.LastName),
            new Claim("workday_id", user.WorkdayId),
            new Claim("alphacode", user.AlphaCode)
        };

        var roles = await _users.GetRolesAsync(user);
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        DateTime expireTime = DateTime.UtcNow;

        if (rememberMe)
        {
            expireTime = expireTime.AddDays(30);
        }
        else
        {
            expireTime = expireTime.AddMinutes(Convert.ToDouble(_config["ExpireInMinutes"]));
        }

        var token = new JwtSecurityToken(
            issuer: _config["Issuer"],
            audience: _config["Audience"],
            claims: claims,
            expires: expireTime,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
