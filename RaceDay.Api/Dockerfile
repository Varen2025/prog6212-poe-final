# TITLE: Building .NET Docker images
# AUTHOR: Microsoft
# DATE: n.d.
# AVAILABLE:
# https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/docker/building-net-docker-images
# DATE ACCESSED: 20 November 2026
#
# Usage: Referenced for the multi-stage build (SDK image to build,
# ASP.NET runtime image to run), the default container port and
# running the container as a non-root user.

# ---- Stage 1: build and publish ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Restore first so this layer is cached until the .csproj changes.
COPY RaceDay.Api/RaceDay.Api.csproj RaceDay.Api/
RUN dotnet restore RaceDay.Api/RaceDay.Api.csproj

COPY RaceDay.Api/ RaceDay.Api/
RUN dotnet publish RaceDay.Api/RaceDay.Api.csproj \
    -c $BUILD_CONFIGURATION -o /app/publish --no-restore /p:UseAppHost=false

# ---- Stage 2: runtime image ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Plain HTTP inside the private Docker network.
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .

# Run as the non-root "app" user that the .NET images provide.
USER $APP_UID

ENTRYPOINT ["dotnet", "RaceDay.Api.dll"]