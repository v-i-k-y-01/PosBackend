# syntax=docker/dockerfile:1
#
# Multi-stage build for the PosBackend.Api project.
# Build context must be the repository root (so all projects are available):
#   docker build -f src/PosBackend.Api/Dockerfile -t posbackend .
#

# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore each project separately for better Docker layer caching.
COPY ["src/PosBackend.Domain/PosBackend.Domain.csproj", "src/PosBackend.Domain/"]
COPY ["src/PosBackend.Application/PosBackend.Application.csproj", "src/PosBackend.Application/"]
COPY ["src/PosBackend.Infrastructure/PosBackend.Infrastructure.csproj", "src/PosBackend.Infrastructure/"]
COPY ["src/PosBackend.Api/PosBackend.Api.csproj", "src/PosBackend.Api/"]
RUN dotnet restore "src/PosBackend.Api/PosBackend.Api.csproj"

# Copy the rest of the source and publish a release build.
COPY . .
WORKDIR "/src/src/PosBackend.Api"
RUN dotnet publish "PosBackend.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "PosBackend.Api.dll"]
