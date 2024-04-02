using System.Text.Json.Serialization;

namespace MaskinportenAuthentication.Models;

public class AuthenticationResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; }

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("scope")]
    public string Scope { get; set; }

    public override string ToString()
    {
        return $"{nameof(AccessToken)}: {AccessToken}, {nameof(TokenType)}: {TokenType}, {nameof(ExpiresIn)}: {ExpiresIn}, {nameof(Scope)}: {Scope}";
    }
}