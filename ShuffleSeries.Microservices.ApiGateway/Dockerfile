FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release

WORKDIR /src
COPY ["ShuffleSeries.Microservices.ApiGateway/ShuffleSeries.Microservices.ApiGateway.csproj", "ShuffleSeries.Microservices.ApiGateway/"]
RUN dotnet restore "ShuffleSeries.Microservices.ApiGateway/ShuffleSeries.Microservices.ApiGateway.csproj"

COPY . .
WORKDIR "/src/ShuffleSeries.Microservices.ApiGateway"
RUN dotnet build "./ShuffleSeries.Microservices.ApiGateway.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ShuffleSeries.Microservices.ApiGateway.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ShuffleSeries.Microservices.ApiGateway.dll"]
