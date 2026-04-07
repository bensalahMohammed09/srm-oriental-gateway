ARG PROMTAIL_VERSION
FROM grafana/promtail:${PROMTAIL_VERSION}

# Technical labels
LABEL component="Log-shipper"
LABEL project="SRM-Oriental-Gateway"

# Variable for the custom port
ARG PROMTAIL_INTERNAL_PORT

# Copy the custom configuration file into the container
COPY infra/promtail/promtail-config.yml /etc/promtail/promtail-config.yaml

# Promtail requires access to host logs.
# In a highlu secured environment we would use ACLs.

EXPOSE ${PROMTAIL_INTERNAL_PORT}

CMD [ "-config.file=/etc/promtail/promtail-config.yaml" ]