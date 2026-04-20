ARG POSTGRES_VERSION
FROM postgres:${POSTGRES_VERSION}

LABEL component="Database"
LABEL project="SRM-Oriental-Gateway"

# We don't need to manually set ENV here if they are in Compose, 
# but keeping them doesn't hurt.
# DO NOT add USER postgres here; the entrypoint needs root to 
# initialize permissions before it drops to the postgres user itself.


# The port is handled by PGPORT in the environment variables