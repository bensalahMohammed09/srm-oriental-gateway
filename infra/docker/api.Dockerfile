# --- 1. Build stage ---
# Using ARG for versioning to avoid hard-typing
ARG DOTNET_VERSION
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src

# Optimization: Restore Nuget packages using dynamic paths
COPY ["src/backend/Srm.Gateway.Api.csproj", "backend/Srm.Gateway.Api/"]
RUN dotnet restore "backend/Srm.Gateway.Api/Srm.Gateway.Api.csproj"

# Copy source code
COPY src/backend/ .

# Publish the application in Release mode 
WORKDIR "src/backend/Srm.Gateway.Api"
RUN dotnet publish "Srm.Gateway.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# --- 2. Runtime stage ---
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS runtime
WORKDIR /app

# Variables for ports and environment
ARG API_INTERNAL_PORT
ARG API_ENV

# Security & Best Practice: Set environment and dynamic port
ENV ASPNETCORE_ENVIRONMENT=${API_ENV}
ENV ASPNETCORE_URLS=http://+:${API_INTERNAL_PORT}

# Inform docker about the non-standard port
EXPOSE ${API_INTERNAL_PORT}

# Use the built-in non-root user for industrial security
USER $APP_UID

# Copy the published output from the build stage
COPY --from=build /app/publish .

# Set the entry point to run the application

ENTRYPOINT ["dotnet", "Srm.Gateway.Api.dll"]