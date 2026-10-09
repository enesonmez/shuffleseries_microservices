FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ShuffleSeries.Identity/ShuffleSeries.Identity.Api/ShuffleSeries.Identity.Api.csproj", "ShuffleSeries.Identity/ShuffleSeries.Identity.Api/"]
COPY ["ShuffleSeries.Identity/ShuffleSeries.Identity.Application/ShuffleSeries.Identity.Application.csproj", "ShuffleSeries.Identity/ShuffleSeries.Identity.Application/"]
COPY ["ShuffleSeries.Identity/ShuffleSeries.Identity.Domain/ShuffleSeries.Identity.Domain.csproj", "ShuffleSeries.Identity/ShuffleSeries.Identity.Domain/"]
COPY ["ShuffleSeries.Identity/ShuffleSeries.Identity.Infrastructure/ShuffleSeries.Identity.Infrastructure.csproj", "ShuffleSeries.Identity/ShuffleSeries.Identity.Infrastructure/"]
COPY ["ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Application/ShuffleSeries.Shared.Core.Application.csproj", "ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Application/"]
COPY ["ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Exceptions/ShuffleSeries.Shared.Core.Exceptions.csproj", "ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Exceptions/"]
COPY ["ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Domain/ShuffleSeries.Shared.Core.Domain.csproj", "ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Domain/"]
COPY ["ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Infrastructure/ShuffleSeries.Shared.Core.Infrastructure.csproj", "ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Infrastructure/"]
COPY ["ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Web/ShuffleSeries.Shared.Core.Web.csproj", "ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Web/"]
RUN dotnet restore "ShuffleSeries.Identity/ShuffleSeries.Identity.Api/ShuffleSeries.Identity.Api.csproj"
COPY ["ShuffleSeries.Identity/", "ShuffleSeries.Identity/"]
COPY ["ShuffleSeries.Shared.Core/", "ShuffleSeries.Shared.Core/"]
WORKDIR "/src/ShuffleSeries.Identity/ShuffleSeries.Identity.Api"
RUN dotnet build "./ShuffleSeries.Identity.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ShuffleSeries.Identity.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
USER app
ENTRYPOINT ["dotnet", "ShuffleSeries.Identity.Api.dll"]
