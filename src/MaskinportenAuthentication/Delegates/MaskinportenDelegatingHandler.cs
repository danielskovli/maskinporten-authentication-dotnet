using System.Net.Http.Headers;
using MaskinportenAuthentication.Models;
using Microsoft.Extensions.DependencyInjection;
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
    private readonly IMaskinportenClient _maskinportenClient;

    /// <summary>
    /// Creates a new instance of <see cref="MaskinportenDelegatingHandler"/>.
    /// </summary>
    /// <param name="scopes">The scopes to claim with Maskinporten</param>
    /// <param name="provider"></param>
    /// <param name="logger"></param>
    public MaskinportenDelegatingHandler(
        IEnumerable<string> scopes,
        IServiceProvider provider,
        ILogger<MaskinportenDelegatingHandler>? logger = default
    )
    {
        _logger = logger ?? provider.GetService<ILoggerFactory>()?.CreateLogger<MaskinportenDelegatingHandler>();
        _scopes = scopes;
        _maskinportenClient = provider.GetRequiredService<IMaskinportenClient>();
    }

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        _logger?.LogDebug("Executing custom `SendAsync` method; injecting authentication headers");
        var auth = await _maskinportenClient.GetAccessToken(_scopes, cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
