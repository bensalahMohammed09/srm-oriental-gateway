# --- GLOBAL ARGS (Provided by .env -> docker-compose) ---
ARG DOTNET_VERSION=9.0
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
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1


# We copy them as root first to configure file system permissions safely.
COPY --from=build-env /out .

# 🛡️ SECURITY HARDENING: Remove write permissions from all executable binaries and assemblies.
# - 'chmod -R a-w .' removes write ('w') permissions for all users (User, Group, Others).
# - 'chmod -R a+rX .' ensures that files remain readable ('r') and directories/executables remain executable ('X').
RUN chmod -R a-w . && \
    chmod -R a+rX .

# Security: Switch to the built-in non-root 'app' user
# The 'app' user can read and execute the assemblies perfectly, but cannot modify/write to them.
USER app

# Document the port
EXPOSE ${API_INTERNAL_PORT}

ENTRYPOINT ["dotnet", "Srm.Gateway.Api.dll"]