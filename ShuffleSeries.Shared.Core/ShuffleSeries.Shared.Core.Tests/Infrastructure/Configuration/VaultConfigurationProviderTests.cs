using System.Net;
using System.Text;
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
            Paths = ["shuffleseries/shared"]
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
            Paths = ["shuffleseries/catalog"]
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
            Paths = ["shuffleseries/missing"]
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
            Paths = ["shuffleseries/missing"]
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
            Paths = ["shuffleseries/shared"]
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
                opts.Paths = ["shuffleseries/shared"];
            }, handler)
            .Build();

        // Assert
        configuration["AppKey"].Should().Be("AppValue");
    }

    [Fact]
    public void AddVault_WithServiceName_ConfiguresStandardHierarchicalPaths()
    {
        // Arrange
        const string vaultJson = """
        {
            "data": {
                "data": {
                    "CatalogKey": "CatalogSecret"
                }
            }
        }
        """;

        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(vaultJson, Encoding.UTF8, "application/json")
        });

        var inMemorySettings = new Dictionary<string, string?>
        {
            ["Vault:Address"] = "http://localhost:8200",
            ["Vault:Token"] = "test-token",
            ["Vault:MountPoint"] = "secret",
            ["Vault:Enabled"] = "true",
            ["Vault:Optional"] = "true",
            ["Vault:Paths:0"] = "custom/path"
        };

        // Act
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .AddVault("catalog", handler)
            .Build();

        // Assert
        configuration["CatalogKey"].Should().Be("CatalogSecret");
    }

    [Fact]
    public void AddVault_WithNullServiceName_ShouldOnlyAddSharedPath()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        var inMemorySettings = new Dictionary<string, string?>
        {
            ["Vault:Enabled"] = "false"
        };

        // Act
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .AddVault(serviceName: null, handler)
            .Build();

        // Assert
        configuration.Should().NotBeNull();
    }

    [Fact]
    public void AddVault_NullBuilder_ShouldThrowArgumentNullException()
    {
        IConfigurationBuilder builder = null!;
        var act1 = () => builder.AddVault(_ => { });
        var act2 = () => builder.AddVault("catalog");

        act1.Should().Throw<ArgumentNullException>();
        act2.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddVault_NullConfigureAction_ShouldThrowArgumentNullException()
    {
        var builder = new ConfigurationBuilder();
        Action<VaultConfigurationOptions> configure = null!;

        var act = () => builder.AddVault(configure);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WhenOptionsIsNull_ShouldThrowArgumentNullException()
    {
        var act = () => new VaultConfigurationProvider(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public void Load_WhenAddressIsEmptyAndOptionalIsTrue_ShouldReturnWithoutThrowing()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var options = new VaultConfigurationOptions
        {
            Enabled = true,
            Optional = true,
            Address = string.Empty,
            Paths = ["shuffleseries/shared"]
        };

        using var provider = new VaultConfigurationProvider(options, handler);

        var act = () => provider.Load();

        act.Should().NotThrow();
        handler.CallCount.Should().Be(0);
    }

    [Fact]
    public void Load_WhenAddressIsEmptyAndOptionalIsFalse_ShouldThrowInvalidOperationException()
    {
        var options = new VaultConfigurationOptions
        {
            Enabled = true,
            Optional = false,
            Address = string.Empty,
            Paths = ["shuffleseries/shared"]
        };

        using var provider = new VaultConfigurationProvider(options);

        var act = () => provider.Load();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Vault Address cannot be null or empty*");
    }

    [Fact]
    public void Load_WhenPathsIsEmpty_ShouldNotMakeHttpRequests()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var options = new VaultConfigurationOptions
        {
            Enabled = true,
            Address = "http://localhost:8200",
            Paths = []
        };

        using var provider = new VaultConfigurationProvider(options, handler);

        provider.Load();

        handler.CallCount.Should().Be(0);
    }

    [Fact]
    public void Load_WhenVaultResponseContainsJsonArray_ShouldFlattenWithIndexedKeys()
    {
        const string vaultJson = """
        {
            "data": {
                "data": {
                    "Cors": {
                        "AllowedOrigins": ["http://localhost:3000", "http://localhost:5001"]
                    }
                }
            }
        }
        """;

        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(vaultJson, Encoding.UTF8, "application/json")
        });

        var options = new VaultConfigurationOptions
        {
            Enabled = true,
            Address = "http://localhost:8200",
            Token = "test-token",
            Paths = ["shuffleseries/shared"]
        };

        using var provider = new VaultConfigurationProvider(options, handler);

        provider.Load();

        provider.TryGet("Cors:AllowedOrigins:0", out var origin0).Should().BeTrue();
        origin0.Should().Be("http://localhost:3000");
        provider.TryGet("Cors:AllowedOrigins:1", out var origin1).Should().BeTrue();
        origin1.Should().Be("http://localhost:5001");
    }

    [Fact]
    public void Load_WhenVaultResponseLacksNestedDataProperty_ShouldIgnoreGracefully()
    {
        const string vaultJson = """{"request_id": "empty-wrapper", "data": {}}""";

        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(vaultJson, Encoding.UTF8, "application/json")
        });

        var options = new VaultConfigurationOptions
        {
            Enabled = true,
            Address = "http://localhost:8200",
            Token = "test-token",
            Paths = ["shuffleseries/shared"]
        };

        using var provider = new VaultConfigurationProvider(options, handler);

        provider.Load();

        provider.TryGet("AnyKey", out _).Should().BeFalse();
    }
}
