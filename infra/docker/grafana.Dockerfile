ARG GRAFANA_VERSION
FROM grafana/grafana:${GRAFANA_VERSION}

# Technical labels
LABEL component="Visualization-Platform"
LABEL project="SRM-Oriental-Gateway"

# Set environment variables for Grafana
ARG GRAFANA_INTERNAL_PORT
ARG GRAFANA_ADMIN_PASSWORD

ENV GF_SERVER_HTTP_PORT=${GRAFANA_INTERNAL_PORT} \
    GF_SECURITY_ADMIN_PASSWORD=${GRAFANA_ADMIN_PASSWORD} \
    GF_ANALYTICS_REPORTING_ENABLED=false \
    GF_AUTH_ANONYMOUS_ENABLED=false

# Copy the provisioning folder to automate data sources setup
COPY infra/grafana/provisioning/ /etc/grafana/provisioning/

# Use non-root user for security
USER grafana

EXPOSE ${GRAFANA_INTERNAL_PORT}

