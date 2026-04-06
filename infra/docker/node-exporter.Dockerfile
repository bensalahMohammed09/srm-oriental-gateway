ARG NODE_EXPORTER_VERSION
FROM prom/node-exporter:${NODE_EXPORTER_VERSION}

# Technical labels
LABEL component="Hardware-Metrics-Collector"
LABEL project="SRM Oriental Gateway"

# Variable for the custom port
ARG NODE_EXPORTER_INTERNAL_PORT

# Node-exporter runs as non-root user by default

EXPOSE ${NODE_EXPORTER_INTERNAL_PORT}

# Overwrite the default entrypoint to use the custom port
ENTRYPOINT ["/bin/node_exporter"]
CMD /bin/node_exporter --web.listen-address=:${NODE_EXPORTER_INTERNAL_PORT}