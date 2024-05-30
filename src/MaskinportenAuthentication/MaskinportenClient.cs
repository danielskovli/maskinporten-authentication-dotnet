using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using MaskinportenAuthentication.Exceptions;
using MaskinportenAuthentication.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace MaskinportenAuthentication;

/// <inheritdoc/>
public sealed class MaskinportenClient : IMaskinportenClient
{
    private readonly ILogger<MaskinportenClient>? _logger;
    private readonly IOptionsMonitor<MaskinportenSettings> _options;
    private readonly IMemoryCache _tokenCache;

    private string TokenUri => _options.CurrentValue.Authority.Trim('/') + "/token";

    private static readonly HttpClient _httpClient =
        new(new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(15) });

    // private static readonly ConcurrentDictionary<string, MaskinportenTokenResponse> _tokenCache = new();

    /// <summary>
    /// Instantiates a new <see cref="MaskinportenClient"/> object.
    /// </summary>
    /// <param name="options">Maskinporten settings.</param>
    /// <param name="tokenCache">A cache instance for authorization tokens.</param>
    /// <param name="logger">Optional logger interface.</param>
    public MaskinportenClient(
        IOptionsMonitor<MaskinportenSettings> options,
        IMemoryCache tokenCache,
        ILogger<MaskinportenClient>? logger = default
    )
    {
        _options = options;
        _tokenCache = tokenCache;
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task<MaskinportenTokenResponse> GetAccessToken(
        IEnumerable<string> scopes,
        CancellationToken cancellationToken = default
    )
    {
        var formattedScopes = FormattedScopes(scopes);

        var result = _tokenCache.GetOrCreate<object>(
            formattedScopes,
            entry =>
            {
                entry.SetSize(1);
                return new Lazy<Task<MaskinportenTokenResponse>>(
                    () =>
                        Task.Run(
                            async () =>
                            {
                                await Task.Yield();

                                var token = await HandleMaskinportenAuthentication(formattedScopes, cancellationToken);

                                return _tokenCache.Set(
                                    formattedScopes,
                                    token,
                                    new MemoryCacheEntryOptions()
                                        .SetSize(1)
                                        .SetAbsoluteExpiration(token.ExpiresAt)
                                        .AddExpirationToken(
                                            new CancellationChangeToken(
                                                new CancellationTokenSource(
                                                    token.ExpiresAt.AddMilliseconds(100) - DateTime.UtcNow
                                                ).Token
                                            )
                                        )
                                );
                            },
                            cancellationToken
                        ),
                    LazyThreadSafetyMode.ExecutionAndPublication
                );
            }
        );

        Debug.Assert(result is MaskinportenTokenResponse or Lazy<Task<MaskinportenTokenResponse>>);
        if (result is Lazy<Task<MaskinportenTokenResponse>> lazy)
        {
            _logger?.LogDebug("Waiting for token request to resolve with Maskinporten");
            return lazy.Value;
        }

        _logger?.LogDebug(
            "Using cached access token which expires at {ExpiresAt}",
            ((MaskinportenTokenResponse)result).ExpiresAt
        );
        return Task.FromResult((MaskinportenTokenResponse)result);
    }

    /// <summary>
    /// Handles the sending of grants requests to Maskinporten
    /// </summary>
    /// <param name="formattedScopes">A single space-separated string containing the scopes to authorize for</param>
    /// <param name="cancellationToken">An optional cancellation token</param>
    /// <returns></returns>
    /// <exception cref="MaskinportenAuthenticationException"></exception>
    private async Task<MaskinportenTokenResponse> HandleMaskinportenAuthentication(
        string formattedScopes,
        CancellationToken cancellationToken = default
    )
    {
        var jwt = GenerateJwtGrant(formattedScopes);
        var payload = GenerateAuthenticationPayload(jwt);

        _logger?.LogDebug(
            "Sending grant request to Maskinporten: {GrantRequest}",
            await payload.ReadAsStringAsync(cancellationToken)
        );

        using var response = await _httpClient.PostAsync(TokenUri, payload, cancellationToken).ConfigureAwait(false);
        var token =
            await ParseServerResponse(response)
            ?? throw new MaskinportenAuthenticationException("Invalid response from Maskinporten");

        _logger?.LogDebug("Token retrieved successfully");
        return token;
    }

    /// <summary>
    /// Generates a JWT grant for the supplied scope claims along with the pre-configured client id and private key.
    /// </summary>
    /// <param name="formattedScopes">A space-separated list of scopes to make a claim for.</param>
    /// <returns><inheritdoc cref="JsonWebTokenHandler.CreateToken(SecurityTokenDescriptor)"/></returns>
    /// <exception cref="MaskinportenConfigurationException">Missing or invalid Maskinporten configuration.</exception>
    private string GenerateJwtGrant(string formattedScopes)
    {
        MaskinportenSettings? settings;
        try
        {
            settings = _options.CurrentValue;
        }
        catch (OptionsValidationException e)
        {
            throw new MaskinportenConfigurationException(
                $"Error reading MaskinportenSettings from the current app configuration",
                e
            );
        }

        var now = DateTime.UtcNow;
        var expiry = now.AddMinutes(2);
        var jwtDescriptor = new SecurityTokenDescriptor
        {
            Issuer = settings.ClientId,
            Audience = settings.Authority,
            IssuedAt = now,
            Expires = expiry,
            SigningCredentials = new SigningCredentials(settings.Key, SecurityAlgorithms.RsaSha256),
            Claims = new Dictionary<string, object> { ["scope"] = formattedScopes, ["jti"] = Guid.NewGuid().ToString() }
        };

        return new JsonWebTokenHandler().CreateToken(jwtDescriptor);
    }

    /// <summary>
    /// Generates an authentication payload from the supplied JWT (see <see cref="GenerateJwtGrant"/>).<br/><br/>
    /// This payload needs to be a <see cref="FormUrlEncodedContent"/> object with some precise parameters,
    /// as per <a href="https://docs.digdir.no/docs/Maskinporten/maskinporten_guide_apikonsument#5-be-om-token">the docs</a>.
    /// </summary>
    /// <param name="jwtAssertion">The JWT token generated by <see cref="GenerateJwtGrant"/>.</param>
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
