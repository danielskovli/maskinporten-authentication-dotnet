using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SampleWebApp.HttpClients.Models;

public record MaskinportenQueryDTO
{
    [JsonPropertyName("client_name")]
    public string ClientName { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("scopes")]
    public string[] Scopes { get; set; }

    [JsonPropertyName("authorization_lifetime")]
    public long AuthorizationLifetime { get; set; }

    [JsonPropertyName("access_token_lifetime")]
    public long AccessTokenLifetime { get; set; }

    [JsonPropertyName("refresh_token_lifetime")]
    public long RefreshTokenLifetime { get; set; }

    [JsonPropertyName("refresh_token_usage")]
    public string RefreshTokenUsage { get; set; }

    [JsonPropertyName("frontchannel_logout_session_required")]
    public bool FrontchannelLogoutSessionRequired { get; set; }

    [JsonPropertyName("token_endpoint_auth_method")]
    public string TokenEndpointAuthMethod { get; set; }

    [JsonPropertyName("grant_types")]
    public string[] GrantTypes { get; set; }

    [JsonPropertyName("integration_type")]
    public string IntegrationType { get; set; }

    [JsonPropertyName("application_type")]
    public string ApplicationType { get; set; }

    [JsonPropertyName("sso_disabled")]
    public bool SsoDisabled { get; set; }

    [JsonPropertyName("last_updated")]
    public DateTimeOffset LastUpdated { get; set; }

    [JsonPropertyName("created")]
    public DateTimeOffset Created { get; set; }

    [JsonPropertyName("client_id")]
    public string ClientId { get; set; }

    [JsonPropertyName("client_orgno")]
    public string ClientOrgno { get; set; }

    [JsonPropertyName("active")]
    public bool Active { get; set; }

    [JsonPropertyName("jwks_uri")]
    public string JwksUri { get; set; }
}
