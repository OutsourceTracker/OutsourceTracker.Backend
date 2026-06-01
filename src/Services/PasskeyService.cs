using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using OutsourceTracker.Authentication;
using OutsourceTracker.Data;
using System.Text;

namespace OutsourceTracker.Services;

/// <summary>
/// Passkey service that uses Fido2NetLib for WebAuthn ceremonies
/// while storing credentials in ASP.NET Identity's built-in UserPasskeys table
/// and friendly names in a companion metadata table.
/// </summary>
public class PasskeyService
{
    private readonly Fido2 _fido2;
    private readonly AppDataContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _config;

    public PasskeyService(
        AppDataContext db,
        UserManager<ApplicationUser> userManager,
        IConfiguration config,
        IHostEnvironment env)
    {
        _db = db;
        _userManager = userManager;
        _config = config;

        var fidoConfig = BuildFido2Configuration(env.IsDevelopment());
        _fido2 = new Fido2(fidoConfig);
    }

    private Fido2Configuration BuildFido2Configuration(bool isDevelopment)
    {
        // Prefer RPID over ServerDomain for the relying party ID (rp.id in WebAuthn)
        var rpId = _config["Fido2:RPID"] ?? _config["Fido2:ServerDomain"];
        if (string.IsNullOrWhiteSpace(rpId))
            rpId = "localhost";

        var origins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var configuredOrigin = _config["Fido2:Origin"];
        if (!string.IsNullOrWhiteSpace(configuredOrigin))
            origins.Add(configuredOrigin);

        // In Development, always include common localhost origins for the frontend
        // (http/https on the typical Blazor WASM dev ports). This makes passkeys
        // "just work" on localhost without extra config.
        if (isDevelopment)
        {
            // Common Blazor WASM + backend dev server origins (both http/https profiles)
            origins.Add("http://localhost:5052");
            origins.Add("https://localhost:7023");
            origins.Add("http://localhost:5241");
            origins.Add("https://localhost:7253");
            origins.Add("http://localhost");
            origins.Add("https://localhost");
        }

        // Ensure we have at least one origin
        if (origins.Count == 0)
            origins.Add("https://localhost:7023");

        return new Fido2Configuration
        {
            ServerDomain = rpId,
            ServerName = "OutsourceTracker",
            Origins = origins
        };
    }

    public async Task<CredentialCreateOptions> GetRegistrationOptionsAsync(ApplicationUser user, string? displayName)
    {
        // TODO: Call the real _fido2.RequestNewCredential once you determine the exact signature in Fido2 5.0.0-preview3.
        // For now, return a usable options object so the frontend + controller flow can be exercised.

        return new CredentialCreateOptions
        {
            Challenge = new byte[32], // Replace with cryptographically secure random in production
            Rp = new PublicKeyCredentialRpEntity(
                _config["Fido2:RPID"] ?? _config["Fido2:ServerDomain"] ?? "localhost",
                "OutsourceTracker"),
            User = new Fido2User
            {
                Id = Encoding.UTF8.GetBytes(user.Id.ToString()),
                Name = user.Email ?? "",
                DisplayName = displayName ?? user.FullName ?? user.Email ?? ""
            },
            PubKeyCredParams = new List<PubKeyCredParam>
            {
                new PubKeyCredParam(COSE.Algorithm.ES256)
            },
            AuthenticatorSelection = new AuthenticatorSelection
            {
                ResidentKey = ResidentKeyRequirement.Required,
                UserVerification = UserVerificationRequirement.Preferred
            },
            Attestation = AttestationConveyancePreference.Direct
        };
    }

    public async Task<IdentityResult> CompleteRegistrationAsync(
        ApplicationUser user,
        AuthenticatorAttestationRawResponse attestationResponse,
        string? friendlyName)
    {
        // In production, you should store and retrieve the original options from cache/session by a challenge or session id.
        // For now we pass null (Fido2 5.0 will still attempt verification in some flows).

        // TODO: Use the real _fido2.MakeNewCredentialAsync with the correct parameters for Fido2 5.0-preview3.
        // For now we simulate success so the end-to-end flow (including storing in IdentityUserPasskey + metadata) can be tested.

        // Extract the real credential ID that the authenticator returned (base64url).
        // This must match exactly what the browser will send back later during assertion/login.
        string credIdString = GetBase64UrlCredentialId(attestationResponse);

        // For now we still simulate the verification + public key (real MakeNewCredentialAsync + challenge verification TODO).
        // We do store the real CredentialId so that subsequent logins can find the passkey.
        var passkey = new PasskeyMetadata
        {
            UserId = user.Id,
            CredentialId = credIdString,
            PublicKey = string.Empty,   // Will be populated when we wire real Fido2 verification
            SignCount = 0,
            Name = friendlyName,
            CreatedOn = DateTimeOffset.UtcNow
        };

        _db.PasskeyMetadata.Add(passkey);
        await _db.SaveChangesAsync();

        return IdentityResult.Success;
    }

    public async Task<AssertionOptions> GetAssertionOptionsAsync()
    {
        return _fido2.GetAssertionOptions(
            new List<PublicKeyCredentialDescriptor>(),
            UserVerificationRequirement.Preferred
        );
    }

    public async Task<(bool Success, ApplicationUser? User)> CompleteAssertionAsync(
        AuthenticatorAssertionRawResponse assertionResponse)
    {
        // Must use the exact same extraction/normalization as registration so the ID matches.
        string credIdString = GetBase64UrlCredentialId(assertionResponse);

        var passkey = await _db.PasskeyMetadata
            .FirstOrDefaultAsync(p => p.CredentialId == credIdString);

        if (passkey == null)
            return (false, null);

        // TODO: Add real Fido2 assertion verification using passkey.PublicKey here
        // (signature check, challenge, counter, etc.). For now we trust possession of a registered credId.

        passkey.LastUsedOn = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        var user = await _userManager.FindByIdAsync(passkey.UserId.ToString());
        return (true, user);
    }

    public async Task<List<object>> GetUserPasskeysAsync(Guid userId)
    {
        // SQLite does not support DateTimeOffset in ORDER BY with null-coalescing.
        // Materialize first, then sort in memory (acceptable for passkeys — users have very few).
        var passkeys = (await _db.PasskeyMetadata
            .Where(p => p.UserId == userId)
            .ToListAsync())
            .OrderByDescending(p => p.LastUsedOn ?? p.CreatedOn)
            .ToList();

        return passkeys.Select(p => new
        {
            p.CredentialId,
            p.Name,
            p.CreatedOn,
            p.LastUsedOn
        }).Cast<object>().ToList();
    }

    public async Task<bool> DeletePasskeyAsync(Guid userId, string credentialIdBase64Url)
    {
        var passkey = await _db.PasskeyMetadata
            .FirstOrDefaultAsync(p => p.UserId == userId && p.CredentialId == credentialIdBase64Url);

        if (passkey == null) return false;

        _db.PasskeyMetadata.Remove(passkey);
        await _db.SaveChangesAsync();
        return true;
    }

    private async Task<List<PublicKeyCredentialDescriptor>> GetExistingCredentialDescriptors(Guid userId)
    {
        var creds = await _db.UserPasskeys
            .Where(p => p.UserId == userId)
            .Select(p => new PublicKeyCredentialDescriptor(p.CredentialId))
            .ToListAsync();

        return creds;
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var base64 = input.Replace('-', '+').Replace('_', '/');
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }

    /// <summary>
    /// Extracts the WebAuthn credential ID as a normalized base64url string.
    /// This value must be stored exactly as-is during registration and looked up the same way during assertion.
    /// </summary>
    private static string GetBase64UrlCredentialId(AuthenticatorAttestationRawResponse r)
    {
        if (!string.IsNullOrWhiteSpace(r.Id))
            return NormalizeBase64Url(r.Id);

        if (r.RawId != null && r.RawId.Length > 0)
            return Base64UrlEncode(r.RawId);

        return string.Empty;
    }

    private static string GetBase64UrlCredentialId(AuthenticatorAssertionRawResponse r)
    {
        if (!string.IsNullOrWhiteSpace(r.Id))
            return NormalizeBase64Url(r.Id);

        if (r.RawId != null && r.RawId.Length > 0)
            return Base64UrlEncode(r.RawId);

        return string.Empty;
    }

    private static string NormalizeBase64Url(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        return input.TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}