using MaskinportenAuthentication.Models;

namespace MaskinportenAuthentication;

public interface IMaskinportenClient
{
    /// <summary>
    /// Provides access to a http client instance which can be used to send requests and receive responses.
    /// The lifecycle of this instance is static/singleton, and is read-only for consumers.
    /// </summary>
    public HttpClient HttpClient { get; }

    /// <summary>
    /// Sends an authorization request to Maskinporten and retrieves JWT Bearer tokens for successful request.<br/><br/>
    /// Will cache tokens per scope, for the lifetime duration as defined in the token Maskinporten token payload,
    /// which means this method is safe to call in a loop or concurrent environment without encountering rate concerns.
    /// </summary>
    /// <param name="scopes">A list of scopes to claim authorization for for</param>
    /// <param name="cancellationToken">An optional cancellation token to be forwarded to internal http calls</param>
    /// <returns></returns>
    public Task<MaskinportenTokenResponse> Authorize(
        IEnumerable<string> scopes,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Factory method for creating a pre-authorized http request
    /// </summary>
    /// <param name="scopes">A list of scopes to claim authorization for for</param>
    /// <param name="method">Http method (eg. GET, POST, etc)</param>
    /// <param name="uri">URI to bind the request to</param>
    /// <param name="cancellationToken">An optional cancellation token to be forwarded to internal http calls</param>
    /// <returns></returns>
    public Task<HttpRequestMessage> AuthorizedRequestAsync(
        IEnumerable<string> scopes,
        HttpMethod method,
        string uri,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Factory method for creating a pre-authorized http request
    /// </summary>
    /// <param name="scopes">A list of scopes to claim authorization for for</param>
    /// <param name="configureRequest">An optional action delegate used to configure the http request (URI, method, content-type, etc)</param>
    /// <param name="cancellationToken">An optional cancellation token to be forwarded to internal http calls</param>
    /// <returns></returns>
    public Task<HttpRequestMessage> AuthorizedRequestAsync(
        IEnumerable<string> scopes,
        Action<HttpRequestMessage>? configureRequest = default,
        CancellationToken cancellationToken = default
    );
}
