# https://aka.ms/customizecontainer - for more info on debugging containers

# Base runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release

WORKDIR /src

# Copy csproj + nuget.config first → best caching
COPY ["OutsourceTracker.Backend.csproj", "./"]
COPY ["nuget.config", "./"]

# Restore using the config file (no need to repeat --source)
RUN dotnet restore "OutsourceTracker.Backend.csproj" \
    --configfile "nuget.config"

# Copy source code
COPY . .

# Build
RUN dotnet build "OutsourceTracker.Backend.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/build \
    --no-restore

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release

RUN dotnet publish "OutsourceTracker.Backend.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# Final image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OutsourceTracker.Backend.dll"]