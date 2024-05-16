using System.Net.Http.Headers;
using MaskinportenAuthentication.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MaskinportenAuthentication.Delegates;

public sealed class MaskinportenDelegatingHandler : DelegatingHandler
{
    private readonly ILogger<MaskinportenDelegatingHandler>? _logger;
    private readonly IEnumerable<string> _scopes;
    private readonly MaskinportenClient _maskinportenClient;

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
