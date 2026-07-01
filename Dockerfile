# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files and restore (better layer caching)
COPY DevOps.WebAPI/*.csproj ./DevOps.WebAPI/
RUN dotnet restore DevOps.WebAPI/DevOps.WebAPI.csproj

# Copy everything else and publish
COPY DevOps.WebAPI/. ./DevOps.WebAPI/
WORKDIR /src/DevOps.WebAPI
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
COPY docker-entrypoint.sh /usr/local/bin/docker-entrypoint.sh
RUN mkdir -p /app/logs \
    && chown -R $APP_UID:$APP_UID /app \
    && chmod +x /usr/local/bin/docker-entrypoint.sh

# Render injects PORT env var; bind to it
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

USER root
ENTRYPOINT ["docker-entrypoint.sh"]
CMD ["dotnet", "DevOps.WebAPI.dll"]