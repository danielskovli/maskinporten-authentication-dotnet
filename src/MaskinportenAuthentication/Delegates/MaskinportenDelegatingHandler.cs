using System.Net.Http.Headers;
using MaskinportenAuthentication.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MaskinportenAuthentication.Delegates;

/// <summary>
/// A <see cref="DelegatingHandler"/> (middleware) that provides authorization for all http requests
/// </summary>
internal sealed class MaskinportenDelegatingHandler : DelegatingHandler
{
    private readonly ILogger<MaskinportenDelegatingHandler>? _logger;
    private readonly IEnumerable<string> _scopes;
    private readonly MaskinportenClient _maskinportenClient;

    /// <summary>
    /// Creates a new instance of <see cref="MaskinportenDelegatingHandler"/>.
    /// </summary>
    /// <param name="scopes">The scopes to claim with Maskinporten</param>
    /// <param name="settings">Optional <see cref="MaskinportenSettings"/> object used to configure the underlying <see cref="MaskinportenClient"/>.
    /// Can be omitted if the client has previously been configured.</param>
    /// <param name="loggerFactory">Optional logger factory interface.</param>
    public MaskinportenDelegatingHandler(
        IEnumerable<string> scopes,
        MaskinportenSettings? settings = default,
        ILoggerFactory? loggerFactory = default
    )
    {
        _logger = loggerFactory?.CreateLogger<MaskinportenDelegatingHandler>();
        _scopes = scopes;
        _maskinportenClient = new MaskinportenClient(
            settings: settings,
            logger: loggerFactory?.CreateLogger<MaskinportenClient>()
        );
    }

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        _logger?.LogDebug("Executing custom `SendAsync` method; injecting authentication headers");
        var auth = await _maskinportenClient.Authorize(_scopes, cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
