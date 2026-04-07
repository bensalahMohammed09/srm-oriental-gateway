# --- 1. Build stage ---
# Using ARG for versioning to avoid hard-typing
ARG DOTNET_VERSION
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /app

# 1. Copy the project file specifically for restore (better caching)
# Local path: src/backend/Srm.Gateway.Api/Srm.Gateway.Api.csproj
COPY ["src/backend/Srm.Gateway.Api/Srm.Gateway.Api.csproj", "backend/Srm.Gateway.Api/"]

# 2. Restore dependencies
RUN dotnet restore "backend/Srm.Gateway.Api/Srm.Gateway.Api.csproj"

# 3. Copy the entire backend source
COPY src/backend/ .

# 4. Set WORKDIR to where the API project now sits inside the container
# Since we copied to '.', the 'Srm.Gateway.Api' folder is in the current directory
WORKDIR "/src/backend/Srm.Gateway.Api"
RUN dotnet publish  -c Release -o /out

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
COPY --from=build /out .

# Set the entry point to run the application

ENTRYPOINT ["dotnet", "Srm.Gateway.Api.dll"]