# Using a specific stable version for industrial consistency
ARG PROMETHEUS_VERSION
FROM prom/prometheus:${PROMETHEUS_VERSION}

# Technical labels
LABEL component="Monitoring-System"
LABEL project="SRM-Oriental-Gateway"

# Copying the custom configuration file into the container
COPY infra/prometheus/prometheus.yml /etc/prometheus/prometheus.yml

# Variable for the custom port
ARG PROMETHEUS_INTERNAL_PORT

# Prometheus image runs as nobody user by default
# we keep this for industrial security standards

# Overwrite the default entrypoint to use the custom port
ENTRYPOINT ["/bin/prometheus"]
CMD /bin/prometheus --config.file=/etc/prometheus/prometheus.yml --storage.tsdb.path=/prometheus --web.listen-address=:${PROMETHEUS_INTERNAL_PORT}

# Expose the non-standard port 
EXPOSE ${PROMETHEUS_INTERNAL_PORT}