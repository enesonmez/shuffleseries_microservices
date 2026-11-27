FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ShuffleSeries.ShuffleEngine/ShuffleSeries.ShuffleEngine.Api/ShuffleSeries.ShuffleEngine.Api.csproj", "ShuffleSeries.ShuffleEngine/ShuffleSeries.ShuffleEngine.Api/"]
COPY ["ShuffleSeries.ShuffleEngine/ShuffleSeries.ShuffleEngine.Application/ShuffleSeries.ShuffleEngine.Application.csproj", "ShuffleSeries.ShuffleEngine/ShuffleSeries.ShuffleEngine.Application/"]
COPY ["ShuffleSeries.ShuffleEngine/ShuffleSeries.ShuffleEngine.Domain/ShuffleSeries.ShuffleEngine.Domain.csproj", "ShuffleSeries.ShuffleEngine/ShuffleSeries.ShuffleEngine.Domain/"]
COPY ["ShuffleSeries.ShuffleEngine/ShuffleSeries.ShuffleEngine.Infrastructure/ShuffleSeries.ShuffleEngine.Infrastructure.csproj", "ShuffleSeries.ShuffleEngine/ShuffleSeries.ShuffleEngine.Infrastructure/"]
COPY ["ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Application/ShuffleSeries.Shared.Core.Application.csproj", "ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Application/"]
COPY ["ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Exceptions/ShuffleSeries.Shared.Core.Exceptions.csproj", "ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Exceptions/"]
COPY ["ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Domain/ShuffleSeries.Shared.Core.Domain.csproj", "ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Domain/"]
COPY ["ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Infrastructure/ShuffleSeries.Shared.Core.Infrastructure.csproj", "ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Infrastructure/"]
COPY ["ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Web/ShuffleSeries.Shared.Core.Web.csproj", "ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Web/"]
RUN dotnet restore "ShuffleSeries.ShuffleEngine/ShuffleSeries.ShuffleEngine.Api/ShuffleSeries.ShuffleEngine.Api.csproj"
COPY ["ShuffleSeries.ShuffleEngine/", "ShuffleSeries.ShuffleEngine/"]
COPY ["ShuffleSeries.Shared.Core/", "ShuffleSeries.Shared.Core/"]
WORKDIR "/src/ShuffleSeries.ShuffleEngine/ShuffleSeries.ShuffleEngine.Api"
RUN dotnet build "./ShuffleSeries.ShuffleEngine.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ShuffleSeries.ShuffleEngine.Api.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
USER app
ENTRYPOINT ["dotnet", "ShuffleSeries.ShuffleEngine.Api.dll"]
