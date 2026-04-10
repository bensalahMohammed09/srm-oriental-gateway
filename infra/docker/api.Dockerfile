# --- 1. Étape de Build ---
ARG DOTNET_VERSION
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build

# On utilise /app comme racine unique
WORKDIR /app

# 1. On copie l'intégralité du contexte
COPY . .

# 2. VÉRIFICATION DU CHEMIN (Debug)
# On s'assure que le fichier projet est bien là
RUN ls -la src/backend/Srm.Gateway.Api/Srm.Gateway.Api.csproj

# 3. PUBLICATION
# On utilise le chemin complet depuis la racine /app
# On change le dossier de sortie pour /publish (racine du conteneur)
RUN dotnet publish "src/backend/Srm.Gateway.Api/Srm.Gateway.Api.csproj" \
    -c Release \
    -o /publish \
    /p:UseAppHost=false \
    --no-cache \
    --self-contained false

# 4. VÉRIFICATION DE SÉCURITÉ
# Si la DLL n'est pas générée, on affiche l'erreur et on arrête tout
RUN ls -la /publish && \
    if [ ! -f /publish/Srm.Gateway.Api.dll ]; then \
        echo "-------------------------------------------------------"; \
        echo "ERREUR : La DLL Srm.Gateway.Api.dll est absente de /publish"; \
        echo "Contenu de /publish :"; \
        ls -R /publish; \
        echo "-------------------------------------------------------"; \
        exit 1; \
    fi

# --- 2. Étape de Runtime ---
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS runtime
WORKDIR /app

ARG API_INTERNAL_PORT
ARG API_ENV

ENV ASPNETCORE_ENVIRONMENT=${API_ENV}
ENV ASPNETCORE_URLS=http://+:${API_INTERNAL_PORT}

EXPOSE ${API_INTERNAL_PORT}

# Sécurité .NET 8/9
USER $APP_UID

# On récupère le résultat validé
COPY --from=build /publish .

ENTRYPOINT ["dotnet", "Srm.Gateway.Api.dll"]