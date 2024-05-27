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
    /// <param name="options"></param>
    /// <param name="tokenCache"></param>
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
        var cachedToken = _tokenCache.GetOrCreate(
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
                                _logger?.LogDebug("Cached token is not available or has expired, re-authenticating");
                                var jwt = GenerateJwtGrant(formattedScopes);
                                var payload = GenerateAuthenticationPayload(jwt);
                                using var response = await _httpClient
                                    .PostAsync(TokenUri, payload, cancellationToken)
                                    .ConfigureAwait(false);

                                var token = await ParseServerResponse(response);
                                _logger?.LogDebug("Token retrieved successfully");

                                // TODO: This can be simplified/mostly skipped. Keeping for debug while WIP
                                _tokenCache.Set(
                                    formattedScopes,
                                    token,
                                    new MemoryCacheEntryOptions
                                    {
                                        Size = 1,
                                        AbsoluteExpiration = token.ExpiresAt,
                                        ExpirationTokens =
                                        {
                                            new CancellationChangeToken(
                                                new CancellationTokenSource(
                                                    token.ExpiresAt.AddMilliseconds(100) - DateTime.UtcNow
                                                ).Token
                                            )
                                        },
                                        PostEvictionCallbacks =
                                        {
                                            new PostEvictionCallbackRegistration
                                            {
                                                EvictionCallback = (key, value, reason, state) =>
                                                {
                                                    _logger?.LogDebug(
                                                        "Eviction event from MemoryCache: Key={Key}, Value={Value}, Reason={Reason}, State={State}, TokenExpiryDiff={TokenExpiryDiff}",
                                                        key,
                                                        value,
                                                        reason,
                                                        state,
                                                        DateTime.UtcNow - ((MaskinportenTokenResponse?)value)?.ExpiresAt
                                                    );
                                                },
                                                State = null
                                            }
                                        }
                                    }
                                );
                                return token;
                            },
                            cancellationToken
                        ),
                    LazyThreadSafetyMode.ExecutionAndPublication
                );
            }
        );

        return cachedToken is not null ? cachedToken.Value : throw new MaskinportenAuthenticationException();

        // OLD IMPLEMENTATION BELOW:
        // if (
        //     _tokenCache.TryGetValue(formattedScopes, out MaskinportenTokenResponse? cachedToken)
        //     && cachedToken is not null
        // )
        // {
        //     _logger?.LogDebug("Using cached access token which expires at: {ExpiresAt}", cachedToken.ExpiresAt);
        //     return cachedToken;
        // }
        //
        // _logger?.LogDebug("Cached token is not available or has expired, re-authenticating");
        // var jwt = GenerateJwtGrant(formattedScopes);
        // var payload = GenerateAuthenticationPayload(jwt);
        // using var response = await _httpClient.PostAsync(TokenUri, payload, cancellationToken).ConfigureAwait(false);
        //
        // var token = await ParseServerResponse(response);
        // _logger?.LogDebug("Token retrieved successfully");
        //
        // return _tokenCache.Set(
        //     formattedScopes,
        //     token,
        //     new MemoryCacheEntryOptions()
        //         .SetAbsoluteExpiration(token.ExpiresAt)
        //         .AddExpirationToken(
        //             new CancellationChangeToken(
        //                 new CancellationTokenSource(token.ExpiresAt.AddMilliseconds(100) - DateTime.UtcNow).Token
        //             )
        //         )
        //         .RegisterPostEvictionCallback(
        //             (key, value, reason, state) =>
        //             {
        //                 _logger?.LogDebug(
        //                     "Eviction event from MemoryCache: Key={Key}, Value={Value}, Reason={Reason}, State={State}, TokenExpiryDiff={TokenExpiryDiff}",
        //                     key,
        //                     value,
        //                     reason,
        //                     state,
        //                     DateTime.UtcNow - ((MaskinportenTokenResponse?)value)?.ExpiresAt
        //                 );
        //             }
        //         )
        // );
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
