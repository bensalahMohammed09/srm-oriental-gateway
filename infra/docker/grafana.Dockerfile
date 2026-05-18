# On utilise uniquement des ARG pour la construction de l'image
ARG GRAFANA_VERSION
FROM grafana/grafana:${GRAFANA_VERSION}

LABEL component="Visualization-Platform"
LABEL project="SRM-Oriental-Gateway"

# On ne définit PLUS de ENV ici. 
# Toute la configuration se fera au démarrage via le Docker Compose.


USER grafana

# On expose le port (informatif)
ARG GRAFANA_INTERNAL_PORT
EXPOSE ${GRAFANA_INTERNAL_PORT}