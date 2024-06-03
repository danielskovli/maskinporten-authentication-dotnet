using System.Text.Json;
using FluentAssertions;
using MaskinportenAuthentication.Models;

namespace MaskinportenAuthentication.Tests.Models;

public class MaskinportenTokenResponseTest
{
    [Fact]
    public void ShouldDeserializeFromJsonCorrectly()
    {
        // Arrange
        var json = """
            {
                "access_token": "jwt.content.here",
                "token_type": "Bearer",
                "expires_in": 120,
                "scope": "anything"
            }
            """;

        // Act
        var beforeCreation = DateTime.UtcNow;
        var token = JsonSerializer.Deserialize<MaskinportenTokenResponse>(json);
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.NotNull(token);
        token.AccessToken.Should().Be("jwt.content.here");
        token.TokenType.Should().Be("Bearer");
        token.Scope.Should().Be("anything");
        token.ExpiresIn.Should().Be(120);
        token.ExpiresAt.Should().BeBefore(afterCreation.AddSeconds(120)).And.BeAfter(beforeCreation.AddSeconds(120));
    }
}
