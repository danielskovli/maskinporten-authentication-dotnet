using System.Text.Json.Serialization;

namespace MaskinportenAuthentication.Models;

/// <summary>
/// A Maskinporten response received after a successful grant request.
/// </summary>
public sealed record MaskinportenTokenResponse
{
    /// <summary>
    /// The JWT access token to be used in the Authorization header for downstream requests.
    /// </summary>
    [JsonPropertyName("access_token")]
    public required string AccessToken { get; set; }

    /// <summary>
    /// The type of JWT received. In this context, the value is always `Bearer`.
    /// </summary>
    [JsonPropertyName("token_type")]
    public required string TokenType { get; set; }

    /// <summary>
    /// The number of seconds until token expiry. Typically set to 120 = 2 minutes.
    /// </summary>
    [JsonPropertyName("expires_in")]
    public required int ExpiresIn { get; set; }

    /// <summary>
    /// The scope(s) associated with the authorization token (<see cref="AccessToken"/>).
    /// </summary>
    [JsonPropertyName("scope")]
    public required string Scope { get; set; }

    /// <summary>
    /// Convenience conversion of <see cref="ExpiresIn"/> to an actual instant in time.
    /// Takes the <see cref="_tokenExpirationMargin"/> into account.
    /// </summary>
    public DateTime ExpiresAt => _createdAt.AddSeconds(ExpiresIn - _tokenExpirationMargin);

    /// <summary>
    /// Is the token expired?
    /// Takes the <see cref="_tokenExpirationMargin"/> into account.
    /// </summary>
    public bool IsExpired => ExpiresAt < DateTime.UtcNow;

    /// <summary>
    /// Internal tracker used by <see cref="ExpiresAt"/> to calculate an expiry <see cref="DateTime"/>.
    /// </summary>
    private readonly DateTime _createdAt = DateTime.UtcNow;

    /// <summary>
    /// The margin to take into consideration when determining if a token has expired or not (seconds).
    /// </summary>
    private const int _tokenExpirationMargin = 60;

    public override string ToString()
    {
        return $"{nameof(AccessToken)}: {AccessToken}, {nameof(TokenType)}: {TokenType}, {nameof(Scope)}: {Scope}, {nameof(ExpiresIn)}: {ExpiresIn}, {nameof(ExpiresAt)}: {ExpiresAt}, {nameof(IsExpired)}: {IsExpired}";
    }
}
