# ---  GLOBAL ARGS (Available across all stages) ---
ARG DOTNET_VERSION
ARG API_INTERNAL_PORT
ARG API_ENV

# --- Stage 1: BUILD ---
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build-env
WORKDIR /app

# Leverage Docker layer caching by copying files individually
# This prevent a full restore if only code changes, not dependencies
COPY Srm.Gateway.sln ./
COPY Srm.Gateway.Domain/*.csproj ./Srm.Gateway.Domain
COPY Srm.Gateway.Infrastructure/*.csproj ./Srm.Gateway.Infrastructure
COPY Srm.Gateway.Application/*.csproj ./Srm.Gateway.Application
COPY Srm.Gateway.Api/*.csproj ./Srm.Gateway.Api

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY . .

# Publish the application
WORKDIR /app/Srm.Gateway.Api
RUN dotnet publish -c Release -o /out --no-restore

# --- Stage 2: RUNTIME
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}
WORKDIR /app

// Environment Variables Injecting to the container from the build environment
ENV ASPNETCORE_ENVIRONMENT=${API_ENV}
ENV ASPNETCORE_HTTP_PORTS=${API_INTERNAL_PORT}

# Install curl for healthchecks then cleanup to reduce attack surface
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/list/*

# Switch to non-root user 
# We use Build-in 'app' user provided by microsoft in .NET images
USER app

# Copy only the necessary artifacts from the build stage 
COPY --from=build-env --chown=app:app /out .

EXPOSE ${API_INTERNAL_PORT}

# Ensure that the container is actually healthy 
HEALTHCHECK --interval=30s --timeout=3s --retries=3 \
    CMD curl -f http://localhost:${API_INTERNAL_PORT}/health || exit 1

ENTRYPOINT ["dotnet","Srm.Gateway.Api.dll"]

