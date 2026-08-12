# syntax=docker/dockerfile:1

# ---- Build stage -------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /source

# Restore first so the layer is cached until the project file changes.
COPY src/FixturesApi.csproj src/
RUN dotnet restore src/FixturesApi.csproj

COPY src/ src/
RUN dotnet publish src/FixturesApi.csproj \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    --no-restore

# ---- Runtime stage -----------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_RUNNING_IN_CONTAINER=true

COPY --from=build /app/publish .

# Non-root user supplied by the .NET base images.
USER $APP_UID
EXPOSE 8080

ENTRYPOINT ["dotnet", "FixturesApi.dll"]
