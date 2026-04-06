# Using the official n8n image
ARG N8N_VERSION
FROM n8nio/n8n:${N8N_VERSION}

# Technical labels 
LABEL component="Automation-Engine"
LABEL project="SRM-Oriental-Gateway"

# Set environment variables for n8n
ARG N8N_ENCRYPTION_KEY
ARG N8N_INTERNAL_PORT

ENV N8N_ENCRYPTION_KEY=${N8N_ENCRYPTION_KEY}
ENV N8N_PORT=${N8N_INTERNAL_PORT}

USER node

EXPOSE ${N8N_INTERNAL_PORT}