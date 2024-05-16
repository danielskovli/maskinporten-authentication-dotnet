using System.Text.Json.Serialization;

namespace MaskinportenAuthentication.Models;

public record MaskinportenTokenResponse : IEquatable<MaskinportenTokenResponse>
{
    [JsonPropertyName("access_token")]
    public required string AccessToken { get; set; }

    [JsonPropertyName("token_type")]
    public required string TokenType { get; set; }

    [JsonPropertyName("expires_in")]
    public required int ExpiresIn { get; set; }

    [JsonPropertyName("scope")]
    public required string Scope { get; set; }

    private readonly DateTime _createdAt = DateTime.UtcNow;
    public DateTime ExpiresAt => _createdAt.AddSeconds(ExpiresIn);

    public override string ToString()
    {
        return $"{nameof(AccessToken)}: {AccessToken}, {nameof(TokenType)}: {TokenType}, {nameof(Scope)}: {Scope}, {nameof(ExpiresIn)}: {ExpiresIn}, {nameof(ExpiresAt)}: {ExpiresAt}";
    }
}
