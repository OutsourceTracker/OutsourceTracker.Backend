using System.ComponentModel.DataAnnotations;

namespace OutsourceTracker.Authentication;

/// <summary>
/// Stores complete passkey credential data + user-friendly name.
/// This is our primary storage for passkeys (works great with JWT auth and gives full control).
/// We no longer rely on Identity's internal IdentityUserPasskey table for credentials.
/// </summary>
public class PasskeyMetadata
{
    [Key]
    public int Id { get; set; }

    public Guid UserId { get; set; }

    /// <summary>
    /// The WebAuthn credential ID (Base64Url encoded).
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string CredentialId { get; set; } = string.Empty;

    /// <summary>
    /// The public key (Base64 encoded).
    /// </summary>
    [Required]
    public string PublicKey { get; set; } = string.Empty;

    /// <summary>
    /// Signature counter (used to prevent replay attacks).
    /// </summary>
    public uint SignCount { get; set; }

    /// <summary>
    /// Transports supported by the authenticator (JSON array, e.g. ["internal","usb"]).
    /// </summary>
    public string? Transports { get; set; }

    /// <summary>
    /// Friendly name chosen by the user (e.g. "iPhone 16", "YubiKey 5").
    /// </summary>
    [MaxLength(100)]
    public string? Name { get; set; }

    public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? LastUsedOn { get; set; }

    // Navigation
    public ApplicationUser? User { get; set; }
}