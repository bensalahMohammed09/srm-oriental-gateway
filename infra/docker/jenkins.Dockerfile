FROM jenkins/jenkins:lts-jdk17

USER root

# 1. Installation des outils et des SDK (.NET 9.0)
# Méthode moderne des keyrings pour Debian 12
RUN apt-get update && apt-get install -y \
    apt-transport-https \
    ca-certificates \
    curl \
    gnupg \
    lsb-release && \
    # --- GESTION DOCKER ---
    curl -fsSL https://download.docker.com/linux/debian/gpg | gpg --dearmor -o /usr/share/keyrings/docker-archive-keyring.gpg && \
    echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/docker-archive-keyring.gpg] https://download.docker.com/linux/debian $(lsb_release -cs) stable" > /etc/apt/sources.list.d/docker.list && \
    # --- GESTION MICROSOFT (.NET) ---
    curl -fsSL https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor -o /usr/share/keyrings/microsoft-archive-keyring.gpg && \
    echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/microsoft-archive-keyring.gpg] https://packages.microsoft.com/debian/12/prod bookworm main" > /etc/apt/sources.list.d/microsoft-prod.list && \
    # --- INSTALLATION FINALE ---
    apt-get update && apt-get install -y \
    docker-ce-cli \
    docker-compose-plugin \
    dotnet-sdk-9.0 && \
    # Nettoyage
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*



# 3. Install SonarScanner as a global tool
RUN dotnet tool install --global dotnet-sonarscanner
ENV PATH="$PATH:/root/.dotnet/tools"

# Install dependencies, add the Trivy GPG key, and set up the repository
RUN apt-get update && apt-get install -y wget apt-transport-https gnupg lsb-release \
    && wget -qO - https://aquasecurity.github.io/trivy-repo/deb/public.key | gpg --dearmor -o /usr/share/keyrings/trivy.gpg \
    && echo "deb [signed-by=/usr/share/keyrings/trivy.gpg] https://aquasecurity.github.io/trivy-repo/deb generic main" | tee /etc/apt/sources.list.d/trivy.list \
    && apt-get update \
    && apt-get install -y trivy

RUN apt-get update && apt-get install -y \
        python3 \
        python3-pip \
        python3-venv \
        && rm -rf /var/lib/apt/list/*

RUN pip3 install bandit --break-system-packages