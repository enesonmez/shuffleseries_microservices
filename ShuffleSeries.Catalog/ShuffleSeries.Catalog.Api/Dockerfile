FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ShuffleSeries.Catalog/ShuffleSeries.Catalog.Api/ShuffleSeries.Catalog.Api.csproj", "ShuffleSeries.Catalog/ShuffleSeries.Catalog.Api/"]
COPY ["ShuffleSeries.Catalog/ShuffleSeries.Catalog.Application/ShuffleSeries.Catalog.Application.csproj", "ShuffleSeries.Catalog/ShuffleSeries.Catalog.Application/"]
COPY ["ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Application/ShuffleSeries.Shared.Core.Application.csproj", "ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Application/"]
COPY ["ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Exceptions/ShuffleSeries.Shared.Core.Exceptions.csproj", "ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Exceptions/"]
COPY ["ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Domain/ShuffleSeries.Shared.Core.Domain.csproj", "ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Domain/"]
COPY ["ShuffleSeries.Catalog/ShuffleSeries.Catalog.Domain/ShuffleSeries.Catalog.Domain.csproj", "ShuffleSeries.Catalog/ShuffleSeries.Catalog.Domain/"]
COPY ["ShuffleSeries.Catalog/ShuffleSeries.Catalog.Infrastructure/ShuffleSeries.Catalog.Infrastructure.csproj", "ShuffleSeries.Catalog/ShuffleSeries.Catalog.Infrastructure/"]
COPY ["ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Web/ShuffleSeries.Shared.Core.Web.csproj", "ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Web/"]
RUN dotnet restore "ShuffleSeries.Catalog/ShuffleSeries.Catalog.Api/ShuffleSeries.Catalog.Api.csproj"
COPY . .
WORKDIR "/src/ShuffleSeries.Catalog/ShuffleSeries.Catalog.Api"
RUN dotnet build "./ShuffleSeries.Catalog.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ShuffleSeries.Catalog.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ShuffleSeries.Catalog.Api.dll"]
