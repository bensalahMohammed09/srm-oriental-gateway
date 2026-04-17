ARG NODE_EXPORTER_VERSION
FROM prom/node-exporter:${NODE_EXPORTER_VERSION}

# Technical Labels
LABEL component="Hardware-Metrics-Collector"
LABEL project="SRM-Oriental-Gateway"

ARG NODE_EXPORTER_INTERNAL_PORT

ENV NODE_EXPORTER_PORT=${NODE_EXPORTER_INTERNAL_PORT}

# add test 

USER nobody

ENTRYPOINT ["/bin/node_exporter"]