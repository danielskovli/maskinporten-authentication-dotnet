using System.Text.Json.Serialization;

namespace MaskinportenAuthentication.Models;

public record MaskinportenErrorResponse
{
    [JsonPropertyName("error")]
    public required string Error { get; set; }

    [JsonPropertyName("error_description")]
    public required string ErrorDescription { get; set; }

    [JsonPropertyName("error_uri")]
    public required Uri ErrorUri { get; set; }
}
