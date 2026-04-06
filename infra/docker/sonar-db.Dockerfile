ARG SONAR_DB_VERSION
FROM postgres:${SONAR_DB_VERSION}

# Technical labels
LABEL component="Quality-Database"
LABEL project="SRM-Oriental-Gateway"

# Environment variables
ARG SONAR_DB_NAME
ARG SONAR_DB_USER
ARG SONAR_DB_PASSWORD
ARG SONAR_DB_INTERNAL_PORT

ENV SONAR_DB_NAME=${SONAR_DB_NAME} \
    SONAR_DB_USER=${SONAR_DB_USER} \
    SONAR_DB_PASSWORD=${SONAR_DB_PASSWORD} \
    SONAR_DB_INTERNAL_PORT=${SONAR_DB_INTERNAL_PORT}

# postgres official image runs as non-root user by default
CMD ["sh", "-c", "postgres -p ${SONAR_DB_INTERNAL_PORT}"]

EXPOSE ${SONAR_DB_INTERNAL_PORT}