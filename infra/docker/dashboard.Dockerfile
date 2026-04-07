# --- GLOBAL ARGS (AVANT LE PREMIER FROM) ---
ARG NODE_VERSION
ARG NGINX_VERSION

# --- 1. Build stage ---
FROM node:${NODE_VERSION} AS build
WORKDIR /app

COPY src/frontend/srm-dashboard/package*.json ./
RUN npm install

COPY src/frontend/srm-dashboard/ .
RUN npm run build

# --- 2. Runtime stage (Nginx) ---
FROM nginx:${NGINX_VERSION} AS final

# On redéclare les variables nécessaires pour ce stage précis
ARG DASHBOARD_PORT_INTERNAL
WORKDIR /usr/share/nginx/html

RUN rm -rf ./*
COPY --from=build /app/dist .
COPY infra/docker/nginx.conf /etc/nginx/conf.d/default.conf

# Remplacement dynamique du port
RUN sed -i "s/LISTEN_PORT/${DASHBOARD_PORT_INTERNAL}/g" /etc/nginx/conf.d/default.conf && \
    touch /var/run/nginx.pid && \
    chown -R nginx:nginx /var/run/nginx.pid /var/cache/nginx /var/log/nginx /usr/share/nginx/html

USER nginx
EXPOSE ${DASHBOARD_PORT_INTERNAL}

CMD ["nginx", "-g", "daemon off;"]