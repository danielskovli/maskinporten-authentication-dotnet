using System.Text.Json;
using MaskinportenAuthentication.Models;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace MaskinportenAuthentication;

public static class Maskinporten
{
    private static HttpClient httpClient = new();

    public static async Task<AuthenticationResponse> Authenticate(
        string authority,
        string appId,
        JsonWebKey jwk,
        params string[] scopes
    )
    {
        var jwt = GenerateJwtAssertion(
            authority: authority,
            appId: appId,
            jwk: jwk,
            scopes: scopes
        );

        var payload = GenerateAuthenticationPayload(jwt);
        var uri = authority.Trim('/') + "/token";
        using var response = await httpClient.PostAsync(uri, payload);

        return await ParseServerResponse(response);
    }

    private static string GenerateJwtAssertion(
        string authority,
        JsonWebKey jwk,
        string appId,
        params string[] scopes
    )
    {
        var now = DateTime.UtcNow;
        var expiry = now.AddMinutes(2);
        var jwtDescriptor = new SecurityTokenDescriptor
        {
            Issuer = appId,
            Audience = authority,
            IssuedAt = now,
            Expires = expiry,
            SigningCredentials = new SigningCredentials(jwk, SecurityAlgorithms.RsaSha256),
            Claims = new Dictionary<string, object>
            {
                ["scope"] = String.Join(" ", scopes),
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

    private static async Task<AuthenticationResponse> ParseServerResponse(
        HttpResponseMessage response
    )
    {
        var content = await response.Content.ReadAsStringAsync();

        try
        {
            return response.IsSuccessStatusCode
                ? JsonSerializer.Deserialize<AuthenticationResponse>(content)
                : throw new Models.AuthenticationException(
                    $"Maskinporten authentication failed with status code {response.StatusCode}: {content}"
                );
        }
        catch (JsonException e)
        {
            throw new AuthenticationException(
                $"Maskinporten replied with invalid JSON formatting: {content}",
                e
            );
        }
        catch (Exception e)
        {
            throw new AuthenticationException("Authentication with Maskinporten failed", e);
        }
    }
}
