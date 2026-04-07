FROM jenkins/jenkins:lts-jdk17

USER root

# 1. Installation des outils et des SDK (.NET 9.0)
# Correction : Remplacement de apt-key (déprécié/supprimé) par la méthode moderne des keyrings
RUN apt-get update && apt-get install -y \
    apt-transport-https \
    ca-certificates \
    curl \
    gnupg \
    lsb-release && \
    # --- GESTION DOCKER ---
    # Téléchargement de la clé Docker et configuration du dépôt
    curl -fsSL https://download.docker.com/linux/debian/gpg | gpg --dearmor -o /usr/share/keyrings/docker-archive-keyring.gpg && \
    echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/docker-archive-keyring.gpg] https://download.docker.com/linux/debian $(lsb_release -cs) stable" > /etc/apt/sources.list.d/docker.list && \
    # --- GESTION MICROSOFT (.NET) ---
    # Téléchargement de la clé Microsoft (méthode moderne sans apt-key)
    curl -fsSL https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor -o /usr/share/keyrings/microsoft-archive-keyring.gpg && \
    # Configuration du dépôt Microsoft en pointant sur la clé téléchargée
    echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/microsoft-archive-keyring.gpg] https://packages.microsoft.com/debian/12/prod bookworm main" > /etc/apt/sources.list.d/microsoft-prod.list && \
    # --- INSTALLATION FINALE ---
    apt-get update && apt-get install -y \
    docker-ce-cli \
    docker-compose-plugin \
    dotnet-sdk-9.0 && \
    # Nettoyage
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

RUN groupadd -g 999 docker || true \
    && usermod -aG docker jenkins   

USER jenkins

RUN jenkins-plugin-cli --plugins "workflow-aggregator:latest dark-theme:latest git:latest"