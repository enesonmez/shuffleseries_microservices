FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release

WORKDIR /src
COPY ["ShuffleSeries.ApiGateway/ShuffleSeries.ApiGateway.Api/ShuffleSeries.ApiGateway.Api.csproj", "ShuffleSeries.ApiGateway/ShuffleSeries.ApiGateway.Api/"]
COPY ["ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Web/ShuffleSeries.Shared.Core.Web.csproj", "ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Web/"]
COPY ["ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Application/ShuffleSeries.Shared.Core.Application.csproj", "ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Application/"]
COPY ["ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Domain/ShuffleSeries.Shared.Core.Domain.csproj", "ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Domain/"]
COPY ["ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Exceptions/ShuffleSeries.Shared.Core.Exceptions.csproj", "ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Exceptions/"]
COPY ["ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Infrastructure/ShuffleSeries.Shared.Core.Infrastructure.csproj", "ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Infrastructure/"]
RUN dotnet restore "ShuffleSeries.ApiGateway/ShuffleSeries.ApiGateway.Api/ShuffleSeries.ApiGateway.Api.csproj"

COPY ["ShuffleSeries.ApiGateway/ShuffleSeries.ApiGateway.Api/", "ShuffleSeries.ApiGateway/ShuffleSeries.ApiGateway.Api/"]
COPY ["ShuffleSeries.Shared.Core/", "ShuffleSeries.Shared.Core/"]
WORKDIR "/src/ShuffleSeries.ApiGateway/ShuffleSeries.ApiGateway.Api"
RUN dotnet build "./ShuffleSeries.ApiGateway.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ShuffleSeries.ApiGateway.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
USER app
ENTRYPOINT ["dotnet", "ShuffleSeries.ApiGateway.Api.dll"]
