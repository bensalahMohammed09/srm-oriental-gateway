# --- 1. Build stage ---
FROM node:${NODE_VERSION} AS build
WORKDIR /app

# Copy package files and install dependencies
COPY src/frontend/srm-dashboard/package*.json ./
RUN npm install

# Copy the rest of the frontend source code and build the application
COPY src/frontend/srm-dashboard/ ./
RUN npm run build

# --- 2. Runtime stage ---
ARG NGINX_VERSION
FROM nginx:${NGINX_VERSION}-alpine AS final

ARG DASHBOARD_INTERNAL_PORT
WORKDIR /usr/share/nginx/html

# Clean default assets
RUN rm -rf ./*

# Copy built assets from the build stage
COPY --from=build /app/dist/ .

# Copy  the specialized Nginx configuration
COPY infra/docker/nginx.conf /etc/nginx/conf.d/default.conf

# Replace the internal port placeholder with the .env variable
RUN sed -i "s/\${DASHBOARD_INTERNAL_PORT}/${DASHBOARD_INTERNAL_PORT}/g" /etc/nginx/conf.d/default.conf && \
    touch /var/run/nginx.pid && \
    chown -R nginx:nginx /var/run/nginx.pid /var/cache/nginx /var/log/nginx /usr/share/nginx/html

# Switch to the non-root user provided by the nginx image 
USER nginx

# Expose the internal port for the dashboard
EXPOSE ${DASHBOARD_INTERNAL_PORT}

# Set the entry point to start Nginx in the foreground
CMD ["nginx", "-g", "daemon off;"]
