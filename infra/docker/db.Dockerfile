ARG POSTGRES_VERSION
FROM postgres:${POSTGRES_VERSION}

# Labels for the SRM Gateway project
LABEL component="Database"
LABEL project="SRM-Oriental-Gateway"

# Set environment variables for PostgreSQL
ARG POSTGRES_USER
ARG POSTGRES_PASSWORD
ARG POSTGRES_DB
ARG DB_INTERNAL_PORT

ENV POSTGRES_USER=${POSTGRES_USER} \
    POSTGRES_PASSWORD=${POSTGRES_PASSWORD} \
    POSTGRES_DB=${POSTGRES_DB} \
    DB_INTERNAL_PORT=${DB_INTERNAL_PORT}

# We use CMD to pass the -p flag to postgres entrypoint
CMD ["sh", "-c", "postgres -p ${DB_INTERNAL_PORT}"]

# Copy the 8 tables initialization script 
COPY infra/sql/init.sql /docker-entrypoint-initdb.d/

# Expose the internal port for PostgreSQL
EXPOSE ${DB_INTERNAL_PORT}