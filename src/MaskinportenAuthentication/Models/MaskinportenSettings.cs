using System.Text.Json.Serialization;
using Microsoft.IdentityModel.Tokens;

namespace MaskinportenAuthentication.Models;

/// <summary>
/// A configuration object that represents all required Maskinporten authentication settings.<br/><br/>
/// Typically serialized as `maskinporten-settings.json` and injected in the runtime.
/// </summary>
public record MaskinportenSettings
{
    /// <summary>
    /// The Maskinporten authority/audience to use for authentication and authorization.
    /// More info about environments and URIs <a href="https://docs.digdir.no/docs/Maskinporten/maskinporten_func_wellknown">in the docs</a>.
    /// </summary>
    [JsonPropertyName("authority")]
    public required string Authority { get; set; }

    /// <summary>
    /// The client ID which has been registered with Maskinporten. Typically a uuid4 string.
    /// </summary>
    [JsonPropertyName("clientId")]
    public required string ClientId { get; set; }

    /// <summary>
    /// The private key used to authenticate with Maskinporten, in JWK format.
    /// </summary>
    [JsonPropertyName("key")]
    public required JsonWebKey Key { get; set; }
}
