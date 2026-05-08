ARG PROMTAIL_VERSION
FROM grafana/promtail:${PROMTAIL_VERSION}

LABEL component="Log-Collector-Agent"
LABEL project="SRM-Oriental-Gateway"


# Le port interne est passé via ARG pour la flexibilité
ARG PROMTAIL_INTERNAL_PORT
EXPOSE ${PROMTAIL_INTERNAL_PORT}

# Pas besoin de CMD spécifique, on utilise celui de l'image parente