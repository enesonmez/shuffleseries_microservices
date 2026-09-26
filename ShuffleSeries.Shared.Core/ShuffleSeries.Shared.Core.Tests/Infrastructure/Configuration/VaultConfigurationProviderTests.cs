using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using ShuffleSeries.Shared.Core.Infrastructure.Configuration.Vault;

namespace ShuffleSeries.Shared.Core.Tests.Infrastructure.Configuration;

public class VaultConfigurationProviderTests
{
    private class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;
        public int CallCount { get; private set; }

        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(_responder(request));
        }

        protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            return _responder(request);
        }
    }

    [Fact]
    public void Load_WhenVaultDisabled_DoesNotMakeAnyHttpRequests()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var options = new VaultConfigurationOptions
        {
            Enabled = false,
            Address = "http://localhost:8200",
            Paths = new List<string> { "shuffleseries/shared" }
        };

        using var provider = new VaultConfigurationProvider(options, handler);

        // Act
        provider.Load();

        // Assert
        handler.CallCount.Should().Be(0);
    }

    [Fact]
    public void Load_WhenValidVaultKvV2Response_FlattensNestedAndNormalizedKeys()
    {
        // Arrange
        const string vaultJson = """
        {
            "request_id": "test-req",
            "data": {
                "data": {
                    "ConnectionStrings:Database": "Host=localhost;Port=5432;Database=testdb;",
                    "MessageBroker": {
                        "Host": "rabbitmq-host",
                        "Port": 5672
                    },
                    "Redis__Password": "SuperSecretRedisPassword"
                }
            }
        }
        """;

        var handler = new FakeHttpMessageHandler(req =>
        {
            req.Headers.Contains("X-Vault-Token").Should().BeTrue();
            req.Headers.GetValues("X-Vault-Token").First().Should().Be("test-token");

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(vaultJson, Encoding.UTF8, "application/json")
            };
        });

        var options = new VaultConfigurationOptions
        {
            Enabled = true,
            Address = "http://localhost:8200",
            Token = "test-token",
            Paths = new List<string> { "shuffleseries/catalog" }
        };

        using var provider = new VaultConfigurationProvider(options, handler);

        // Act
        provider.Load();

        // Assert
        provider.TryGet("ConnectionStrings:Database", out var dbConn).Should().BeTrue();
        dbConn.Should().Be("Host=localhost;Port=5432;Database=testdb;");

        provider.TryGet("MessageBroker:Host", out var msgHost).Should().BeTrue();
        msgHost.Should().Be("rabbitmq-host");

        provider.TryGet("MessageBroker:Port", out var msgPort).Should().BeTrue();
        msgPort.Should().Be("5672");

        provider.TryGet("Redis:Password", out var redisPass).Should().BeTrue();
        redisPass.Should().Be("SuperSecretRedisPassword");
    }

    [Fact]
    public void Load_WhenVaultReturns404_AndOptionalIsTrue_DoesNotThrow()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var options = new VaultConfigurationOptions
        {
            Enabled = true,
            Optional = true,
            Address = "http://localhost:8200",
            Paths = new List<string> { "shuffleseries/missing" }
        };

        using var provider = new VaultConfigurationProvider(options, handler);

        // Act
        var act = () => provider.Load();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Load_WhenVaultReturns404_AndOptionalIsFalse_ThrowsInvalidOperationException()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var options = new VaultConfigurationOptions
        {
            Enabled = true,
            Optional = false,
            Address = "http://localhost:8200",
            Paths = new List<string> { "shuffleseries/missing" }
        };

        using var provider = new VaultConfigurationProvider(options, handler);

        // Act
        var act = () => provider.Load();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*was not found*");
    }

    [Fact]
    public void Load_WhenVaultServerThrowsNetworkError_AndOptionalIsTrue_DoesNotThrow()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(_ => throw new HttpRequestException("Connection refused"));
        var options = new VaultConfigurationOptions
        {
            Enabled = true,
            Optional = true,
            Address = "http://localhost:8200",
            Paths = new List<string> { "shuffleseries/shared" }
        };

        using var provider = new VaultConfigurationProvider(options, handler);

        // Act
        var act = () => provider.Load();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void AddVault_ConfigurationBuilder_IntegratesProperly()
    {
        // Arrange
        const string vaultJson = """
        {
            "data": {
                "data": {
                    "AppKey": "AppValue"
                }
            }
        }
        """;

        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(vaultJson, Encoding.UTF8, "application/json")
        });

        // Act
        var configuration = new ConfigurationBuilder()
            .AddVault(opts =>
            {
                opts.Address = "http://localhost:8200";
                opts.Token = "test-token";
                opts.Paths = new List<string> { "shuffleseries/shared" };
            }, handler)
            .Build();

        // Assert
        configuration["AppKey"].Should().Be("AppValue");
    }
}
