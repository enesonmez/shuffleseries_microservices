using FluentAssertions;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using ShuffleSeries.Shared.Core.Web.Swagger;

namespace ShuffleSeries.Shared.Core.Tests.Web.Swagger;

public class SwaggerInfrastructureTests
{
    [Fact]
    public async Task OpenApiSecurityDocumentTransformer_InjectsMetadataAndBearerScheme()
    {
        // Arrange
        var options = new SwaggerOptions
        {
            Title = "Test Microservice API",
            Version = "v2",
            Description = "Test API Description",
            IncludeJwtSecurity = true
        };

        var transformer = new OpenApiSecurityDocumentTransformer(options);
        var document = new OpenApiDocument();
        var context = new OpenApiDocumentTransformerContext
        {
            DocumentName = "v2",
            DescriptionGroups = Array.Empty<Microsoft.AspNetCore.Mvc.ApiExplorer.ApiDescriptionGroup>(),
            ApplicationServices = new ServiceCollection().BuildServiceProvider()
        };

        // Act
        await transformer.TransformAsync(document, context, CancellationToken.None);

        // Assert
        document.Info.Should().NotBeNull();
        document.Info.Title.Should().Be("Test Microservice API");
        document.Info.Version.Should().Be("v2");
        document.Info.Description.Should().Be("Test API Description");

        document.Components.Should().NotBeNull();
        document.Components.SecuritySchemes.Should().ContainKey("Bearer");

        var bearerScheme = document.Components.SecuritySchemes["Bearer"];
        bearerScheme.Type.Should().Be(SecuritySchemeType.Http);
        bearerScheme.Scheme.Should().Be("bearer");
        bearerScheme.BearerFormat.Should().Be("JWT");

        document.Security.Should().NotBeNull();
        document.Security.Should().HaveCount(1);
    }

    [Fact]
    public async Task OpenApiSecurityDocumentTransformer_WhenJwtSecurityDisabled_DoesNotInjectBearerScheme()
    {
        // Arrange
        var options = new SwaggerOptions
        {
            Title = "Public API",
            Version = "v1",
            IncludeJwtSecurity = false
        };

        var transformer = new OpenApiSecurityDocumentTransformer(options);
        var document = new OpenApiDocument();
        var context = new OpenApiDocumentTransformerContext
        {
            DocumentName = "v1",
            DescriptionGroups = Array.Empty<Microsoft.AspNetCore.Mvc.ApiExplorer.ApiDescriptionGroup>(),
            ApplicationServices = new ServiceCollection().BuildServiceProvider()
        };

        // Act
        await transformer.TransformAsync(document, context, CancellationToken.None);

        // Assert
        document.Info.Title.Should().Be("Public API");
        document.Components?.SecuritySchemes.Should().BeNullOrEmpty();
        document.Security.Should().BeNullOrEmpty();
    }

    [Fact]
    public void AddSharedSwagger_RegistersSwaggerOptionsInServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSharedSwagger(options =>
        {
            options.Title = "Catalog API";
            options.Version = "v1";
            options.RoutePrefix = "docs";
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetService<SwaggerOptions>();

        // Assert
        options.Should().NotBeNull();
        options!.Title.Should().Be("Catalog API");
        options.Version.Should().Be("v1");
        options.RoutePrefix.Should().Be("docs");
    }

    [Fact]
    public void SwaggerOptions_DefaultValues_AreExpected()
    {
        // Act
        var options = new SwaggerOptions();

        // Assert
        options.Title.Should().Be("ShuffleSeries API");
        options.Version.Should().Be("v1");
        options.Description.Should().Be("ShuffleSeries Microservices Platform API Documentation");
        options.IncludeJwtSecurity.Should().BeTrue();
        options.EnableOpenApi.Should().BeTrue();
        options.EnableSwaggerUi.Should().BeTrue();
        options.EnableScalarUi.Should().BeTrue();
        options.RoutePrefix.Should().Be("swagger");
    }

    [Fact]
    public void AddSharedSwagger_WithoutConfigureAction_RegistersDefaultOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSharedSwagger();

        var provider = services.BuildServiceProvider();
        var options = provider.GetService<SwaggerOptions>();

        // Assert
        options.Should().NotBeNull();
        options!.Title.Should().Be("ShuffleSeries API");
        options.Version.Should().Be("v1");
    }
}
