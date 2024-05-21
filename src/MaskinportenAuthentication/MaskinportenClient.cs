using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text.Json;
using MaskinportenAuthentication.Exceptions;
using MaskinportenAuthentication.Models;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace MaskinportenAuthentication;

/// <inheritdoc/>
public sealed class MaskinportenClient : IMaskinportenClient
{
    private static readonly int _tokenExpirationMargin = 20;
    private static MaskinportenSettings? _authenticationSettings;
    private readonly ILogger<MaskinportenClient>? _logger;

    private static string TokenUri => _authenticationSettings?.Authority.Trim('/') + "/token";

    public HttpClient HttpClient => _httpClient;
    private static readonly HttpClient _httpClient = new();
    private static readonly ConcurrentDictionary<string, MaskinportenTokenResponse> _tokenCache = new();

    /// <summary>
    /// Instantiates a new <see cref="MaskinportenClient"/> object.
    /// </summary>
    /// <param name="settings">Optional <see cref="MaskinportenSettings"/> configuration.
    /// May also be provided at any time via <see cref="Configure"/>.
    /// Note: This configuration is a singleton -- only one set of instructions can exist across all instances at any given time.
    /// </param>
    /// <param name="logger">Optional logger interface.</param>
    public MaskinportenClient(MaskinportenSettings? settings = default, ILogger<MaskinportenClient>? logger = default)
    {
        _authenticationSettings = settings ?? _authenticationSettings;
        _logger = logger;
    }

    /// <summary>
    /// Configures the Maskinporten handshake for <b>ALL</b> instances of <see cref="MaskinportenClient"/>.
    /// </summary>
    /// <param name="settings">The settings used for configuration, eg. clientId, secret key jwt,
    /// and the Maskinporten authority URI to target.</param>
    public static void Configure(MaskinportenSettings settings)
    {
        _authenticationSettings = settings;
    }

    /// <inheritdoc/>
    public async Task<MaskinportenTokenResponse> Authorize(
        IEnumerable<string> scopes,
        CancellationToken cancellationToken = default
    )
    {
        var formattedScopes = FormattedScopes(scopes);
        var expiryWindow = DateTime.UtcNow.AddSeconds(_tokenExpirationMargin);

        if (
            _tokenCache.TryGetValue(formattedScopes, out MaskinportenTokenResponse? cachedToken)
            && cachedToken.ExpiresAt >= expiryWindow
        )
        {
            _logger?.LogDebug("Using cached access token which expires at: {expiry}", cachedToken.ExpiresAt);
            return cachedToken;
        }

        _logger?.LogDebug("Cached token is not available or has expired, re-authenticating");
        var jwt = GenerateJwtAssertion(formattedScopes);
        var payload = GenerateAuthenticationPayload(jwt);
        using var response = await HttpClient.PostAsync(TokenUri, payload, cancellationToken).ConfigureAwait(false);

        var token = await ParseServerResponse(response);
        _logger?.LogDebug("Token retrieved successfully");
        return _tokenCache.AddOrUpdate(formattedScopes, token, (key, old) => token);
    }

    /// <inheritdoc/>
    public Task<HttpRequestMessage> AuthorizedRequestAsync(
        IEnumerable<string> scopes,
        HttpMethod method,
        string uri,
        CancellationToken cancellationToken = default
    )
    {
        return AuthorizedRequestAsync(
            scopes,
            request =>
            {
                request.Method = method;
                request.RequestUri = new Uri(uri);
            },
            cancellationToken
        );
    }

    /// <inheritdoc/>
    public async Task<HttpRequestMessage> AuthorizedRequestAsync(
        IEnumerable<string> scopes,
        Action<HttpRequestMessage>? configureRequest = default,
        CancellationToken cancellationToken = default
    )
    {
        var request = new HttpRequestMessage();
        configureRequest?.Invoke(request);
        var authToken = await Authorize(scopes, cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken.AccessToken);

        return request;
    }

    /// <summary>
    /// Generates a JWT assertion for the supplied scope claims.
    /// </summary>
    /// <param name="formattedScopes">A space-separated list of scopes to make a claim for.</param>
    /// <returns><inheritdoc cref="JsonWebTokenHandler.CreateToken(SecurityTokenDescriptor)"/></returns>
    /// <exception cref="MaskinportenConfigurationException">Missing Maskinporten configuration, see <see cref="Configure"/>.</exception>
    private static string GenerateJwtAssertion(string formattedScopes)
    {
        if (_authenticationSettings is null)
        {
            throw new MaskinportenConfigurationException(
                $"Missing Maskinporten configuration {nameof(_authenticationSettings)}"
            );
        }

        var now = DateTime.UtcNow;
        var expiry = now.AddMinutes(2);
        var jwtDescriptor = new SecurityTokenDescriptor
        {
            Issuer = _authenticationSettings.ClientId,
            Audience = _authenticationSettings.Authority,
            IssuedAt = now,
            Expires = expiry,
            SigningCredentials = new SigningCredentials(_authenticationSettings.Key, SecurityAlgorithms.RsaSha256),
            Claims = new Dictionary<string, object> { ["scope"] = formattedScopes, ["jti"] = Guid.NewGuid().ToString() }
        };

        return new JsonWebTokenHandler().CreateToken(jwtDescriptor);
    }

    /// <summary>
    /// Generates an authentication payload from the supplied JWT (see <see cref="GenerateJwtAssertion"/>).<br/><br/>
    /// This payload needs to be a <see cref="FormUrlEncodedContent"/> object with some precise parameters,
    /// as per <a href="https://docs.digdir.no/docs/Maskinporten/maskinporten_guide_apikonsument#5-be-om-token">the docs</a>.
    /// </summary>
    /// <param name="jwtAssertion">The JWT token generated by <see cref="GenerateJwtAssertion"/>.</param>
    private static FormUrlEncodedContent GenerateAuthenticationPayload(string jwtAssertion)
    {
        return new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer",
                ["assertion"] = jwtAssertion
            }
        );
    }

    /// <summary>
    /// Parses the Maskinporten server response and deserializes the JSON body.
    /// </summary>
    /// <param name="httpResponse">The server response.</param>
    /// <returns>A <see cref="MaskinportenTokenResponse"/> for successful requests.</returns>
    /// <exception cref="MaskinportenAuthenticationException">Authentication failed.
    /// This could be caused by an authentication/authorization issue or a myriad of tother circumstances.</exception>
    private static async Task<MaskinportenTokenResponse> ParseServerResponse(HttpResponseMessage httpResponse)
    {
        var content = await httpResponse.Content.ReadAsStringAsync();

        try
        {
            if (!httpResponse.IsSuccessStatusCode)
            {
                throw new MaskinportenAuthenticationException(
                    $"Maskinporten authentication failed with status code {(int)httpResponse.StatusCode} ({httpResponse.StatusCode}): {content}"
                );
            }

            return JsonSerializer.Deserialize<MaskinportenTokenResponse>(content)
                ?? throw new JsonException("JSON body is null");
        }
        catch (JsonException e)
        {
            throw new MaskinportenAuthenticationException(
                $"Maskinporten replied with invalid JSON formatting: {content}",
                e
            );
        }
        catch (Exception e)
        {
            throw new MaskinportenAuthenticationException($"Authentication with Maskinporten failed: {e.Message}", e);
        }
    }

    /// <summary>
    /// Formats a list of scopes according to the expected formatting (space-delimited).
    /// See <a href="https://docs.digdir.no/docs/Maskinporten/maskinporten_guide_apikonsument#5-be-om-token">the docs</a> for more information.
    /// </summary>
    /// <param name="scopes">A list/collection of scopes</param>
    /// <returns>A single string containing the supplied scopes</returns>
    private static string FormattedScopes(IEnumerable<string> scopes) => string.Join(" ", scopes);
}
