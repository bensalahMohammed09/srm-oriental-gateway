# 1. Global Scope Argument
ARG SONAR_DB_VERSION
FROM postgres:${SONAR_DB_VERSION}

# 2. Technical labels for SRM Oriental Gateway
LABEL component="Quality-Database"
LABEL project="SRM-Oriental-Gateway"

# 3. Build Arguments
ARG SONAR_DB_NAME
ARG SONAR_DB_USER
ARG SONAR_DB_PASSWORD
ARG SONAR_DB_INTERNAL_PORT

# 4. Environment Variables
# We use the official POSTGRES_* names so the entrypoint can initialize the DB
ENV POSTGRES_DB=${SONAR_DB_NAME} \
    POSTGRES_USER=${SONAR_DB_USER} \
    POSTGRES_PASSWORD=${SONAR_DB_PASSWORD} \
    # THE FIX: This natively changes the port while keeping the startup logic intact
    PGPORT=${SONAR_DB_INTERNAL_PORT}

# 5. Network documentation
EXPOSE ${SONAR_DB_INTERNAL_PORT}

# NOTE: We DO NOT use 'USER postgres' or 'CMD' here. 
# The official image will:
#   a) Start as root to set permissions on the volume.
#   b) Run 'initdb' to create postgresql.conf (fixing your error).
#   c) Switch to the 'postgres' user automatically.
#   d) Start the server on the port defined in $PGPORT.