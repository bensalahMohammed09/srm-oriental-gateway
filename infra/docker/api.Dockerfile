# --- GLOBAL ARGS (Provided by .env -> docker-compose) ---
ARG DOTNET_VERSION
ARG API_INTERNAL_PORT
ARG API_ENV

# --- STAGE 1: BUILD ---
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build-env
WORKDIR /app

# 1. Cache optimization: Copy solution and project files first
COPY Srm.Gateway.sln ./
COPY Srm.Gateway.Domain/Srm.Gateway.Domain.csproj ./Srm.Gateway.Domain/
COPY Srm.Gateway.Application/Srm.Gateway.Application.csproj ./Srm.Gateway.Application/
COPY Srm.Gateway.Infrastructure/Srm.Gateway.Infrastructure.csproj ./Srm.Gateway.Infrastructure/
COPY Srm.Gateway.Api/Srm.Gateway.Api.csproj ./Srm.Gateway.Api/

# 2. Restore dependencies
RUN dotnet restore

# 3. Copy actual source code
COPY . .

# 4. Publish (STAY at /app root to keep project references intact)
RUN dotnet publish Srm.Gateway.Api/Srm.Gateway.Api.csproj \
    -c Release \
    -o /out 

# --- STAGE 2: RUNTIME ---
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}

# Re-declare ARGs to pull them into this stage scope
ARG API_INTERNAL_PORT
ARG API_ENV

WORKDIR /app

# Mapping to ENV for the application runtime
ENV ASPNETCORE_ENVIRONMENT=${API_ENV}
ENV ASPNETCORE_HTTP_PORTS=${API_INTERNAL_PORT}

# Security: Install curl for Healthchecks and clean up cache
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# Security: Switch to the built-in non-root 'app' user
USER app

# Copy artifacts and apply ownership to the 'app' user
COPY --from=build-env --chown=app:app /out .

# Document the port
EXPOSE ${API_INTERNAL_PORT}

# Healthcheck detail for Prometheus/Docker monitoring
HEALTHCHECK --interval=30s --timeout=3s --retries=3 \
    CMD curl -f http://localhost:${API_INTERNAL_PORT}/health || exit 1

ENTRYPOINT ["dotnet", "Srm.Gateway.Api.dll"]