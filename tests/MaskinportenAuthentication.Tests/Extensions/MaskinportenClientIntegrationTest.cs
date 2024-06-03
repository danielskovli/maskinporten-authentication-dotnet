using FluentAssertions;
using MaskinportenAuthentication.Delegates;
using MaskinportenAuthentication.Exceptions;
using MaskinportenAuthentication.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using Moq;

namespace MaskinportenAuthentication.Tests.Extensions;

public class MaskinportenClientIntegrationTests : IClassFixture<MaskinportenClientIntegrationFixture>
{
    private readonly MaskinportenClientIntegrationFixture _fixture;

    public MaskinportenClientIntegrationTests(MaskinportenClientIntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void AddMaskinportenClient_IHostApplicationBuilder_AddsServices()
    {
        // Arrange
        var builder = _fixture.HostApplicationBuilder;

        // Act
        builder.AddMaskinportenClient(validateSettingsLocation: false);
        builder.AddMaskinportenClient(validateSettingsLocation: false);

        // Assert
        _fixture.Services.Should().ContainSingle(sd => sd.ServiceType == typeof(IMaskinportenClient));
    }

    [Fact]
    public void AddMaskinportenClient_IServiceCollection_AddsServices()
    {
        // Arrange
        var services = _fixture.Services;

        // Act
        services.AddMaskinportenClient(config => { });
        services.AddMaskinportenClient(config => { });

        // Assert
        services.Should().ContainSingle(sd => sd.ServiceType == typeof(IMaskinportenClient));
    }

    [Fact]
    public void UseMaskinportenAuthorization_AddsHandler()
    {
        // Arrange
        var scopes = new[] { "scope1", "scope2" };
        var services = new ServiceCollection();
        var mockProvider = new Mock<IServiceProvider>();
        mockProvider
            .Setup(provider => provider.GetService(typeof(IMaskinportenClient)))
            .Returns(new Mock<IMaskinportenClient>().Object);
        mockProvider
            .Setup(provider => provider.GetService(typeof(MaskinportenDelegatingHandler)))
            .Returns(new MaskinportenDelegatingHandler(scopes, mockProvider.Object));

        var mockBuilder = new Mock<IHttpClientBuilder>();
        mockBuilder.Setup(b => b.Services).Returns(services);

        // Act
        mockBuilder.Object.UseMaskinportenAuthorization(scopes);

        // Assert
        mockProvider.Verify(provider => provider.GetService(typeof(IMaskinportenClient)), Times.Once);
        services.Should().ContainSingle(s => s.ServiceType == typeof(IConfigureOptions<HttpClientFactoryOptions>));
    }

    [Fact]
    public void AddMaskinportenClient_ThrowsException_WhenSettingsFileNotFound()
    {
        // Arrange
        var builder = _fixture.HostApplicationBuilder;
        builder.Services.Clear(); // Avoid singleton check

        // Act
        Action act = () => builder.AddMaskinportenClient(validateSettingsLocation: true);

        // Assert
        act.Should().Throw<MaskinportenConfigurationException>();
    }
}

public class MaskinportenClientIntegrationFixture
{
    public IServiceProvider ServiceProvider { get; }
    public IHostApplicationBuilder HostApplicationBuilder { get; }
    public IServiceCollection Services { get; } = new ServiceCollection();

    public MaskinportenClientIntegrationFixture()
    {
        var configManager = new ConfigurationManager();
        var mockProvider = new Mock<IServiceProvider>();
        mockProvider.Setup(p => p.GetService(typeof(IConfiguration))).Returns(configManager);

        var mockBuilder = new Mock<IHostApplicationBuilder>();
        mockBuilder.Setup(b => b.Services).Returns(Services);
        mockBuilder.Setup(b => b.Configuration).Returns(configManager);

        ServiceProvider = mockProvider.Object;
        HostApplicationBuilder = mockBuilder.Object;
    }
}
