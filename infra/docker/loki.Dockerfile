ARG LOKI_VERSION
FROM grafana/loki:${LOKI_VERSION}

LABEL component="Log-Management-System"
LABEL project="SRM-Oriental-Gateway"


# Copy the Loki configuration file into the container
COPY infra/loki/loki-config.yml /etc/loki/local-config.yaml

# Expose the Loki port

ARG LOKI_INTERNAL_PORT

USER loki

EXPOSE ${LOKI_INTERNAL_PORT}

CMD ["-config.file=/etc/loki/local-config.yaml"]