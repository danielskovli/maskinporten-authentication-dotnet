using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text.Json;
using MaskinportenAuthentication.Exceptions;
using MaskinportenAuthentication.Models;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace MaskinportenAuthentication;

public sealed class MaskinportenClient : IMaskinportenClient
{
    private const int _tokenExpirationMargin = 20;
    private static MaskinportenSettings? _authenticationSettings;
    private readonly ILogger<MaskinportenClient>? _logger;

    private static string TokenUri => _authenticationSettings?.Authority.Trim('/') + "/token";
    private static Func<IEnumerable<string>, string> FormattedScopes = x => string.Join(" ", x);

    public HttpClient HttpClient => _httpClient;
    private static readonly HttpClient _httpClient = new();
    private static readonly ConcurrentDictionary<string, MaskinportenTokenResponse> _tokenCache =
        new();

    public MaskinportenClient(
        MaskinportenSettings? settings = default,
        ILogger<MaskinportenClient>? logger = default
    )
    {
        _authenticationSettings = settings ?? _authenticationSettings;
        _logger = logger;
    }

    public static void Configure(MaskinportenSettings settings)
    {
        _authenticationSettings = settings;
    }

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
            _logger?.LogDebug(
                "Using cached access token which expires at: {expiry}",
                cachedToken.ExpiresAt
            );
            return cachedToken;
        }

        _logger?.LogDebug("Cached token is not available or has expired, re-authenticating");
        var jwt = GenerateJwtAssertion(formattedScopes);
        var payload = GenerateAuthenticationPayload(jwt);
        using var response = await HttpClient
            .PostAsync(TokenUri, payload, cancellationToken)
            .ConfigureAwait(false);

        var token = await ParseServerResponse(response);
        _logger?.LogDebug("Token retrieved successfully");
        return _tokenCache.AddOrUpdate(formattedScopes, token, (key, old) => token);
    }

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

    public async Task<HttpRequestMessage> AuthorizedRequestAsync(
        IEnumerable<string> scopes,
        Action<HttpRequestMessage>? configureRequest = default,
        CancellationToken cancellationToken = default
    )
    {
        var request = new HttpRequestMessage();
        configureRequest?.Invoke(request);
        var authToken = await Authorize(scopes, cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            authToken.AccessToken
        );

        return request;
    }

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
            SigningCredentials = new SigningCredentials(
                _authenticationSettings.Key,
                SecurityAlgorithms.RsaSha256
            ),
            Claims = new Dictionary<string, object>
            {
                ["scope"] = formattedScopes,
                ["jti"] = Guid.NewGuid().ToString()
            }
        };

        return new JsonWebTokenHandler().CreateToken(jwtDescriptor);
    }

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

    private static async Task<MaskinportenTokenResponse> ParseServerResponse(
        HttpResponseMessage httpResponse
    )
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
            throw new MaskinportenAuthenticationException(
                $"Authentication with Maskinporten failed: {e.Message}",
                e
            );
        }
    }
}
