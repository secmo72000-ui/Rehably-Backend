# ── Stage 1: Build ────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution + project files first (layer cache)
COPY ["Rehably.API/Rehably.API.csproj",             "Rehably.API/"]
COPY ["Rehably.Application/Rehably.Application.csproj", "Rehably.Application/"]
COPY ["Rehably.Domain/Rehably.Domain.csproj",       "Rehably.Domain/"]
COPY ["Rehably.Infrastructure/Rehably.Infrastructure.csproj", "Rehably.Infrastructure/"]

# Restore packages
RUN dotnet restore "Rehably.API/Rehably.API.csproj"

# Copy everything and build
COPY . .
WORKDIR /src/Rehably.API
RUN dotnet publish "Rehably.API.csproj" -c Release -o /app/publish --no-restore

# ── Stage 2: Runtime ──────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# Render assigns PORT env var — ASP.NET reads ASPNETCORE_URLS
ENV ASPNETCORE_URLS=http://+:${PORT:-8080}
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "Rehably.API.dll"]
