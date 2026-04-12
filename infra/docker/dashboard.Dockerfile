ARG NODE_VERSION
ARG NGINX_VERSION

# --- 1. Build stage ---
FROM --platform=linux/amd64 node:${NODE_VERSION} AS build
WORKDIR /app
COPY src/frontend/srm-dashboard/package*.json ./
RUN npm install
COPY src/frontend/srm-dashboard/ .
RUN npm run build

# --- 2. Runtime stage (Nginx) ---
FROM --platform=linux/amd64 nginx:${NGINX_VERSION} AS final

WORKDIR /usr/share/nginx/html

# Nettoyage et copie du build React
RUN rm -rf ./*
COPY --from=build /app/dist .

# On copie le fichier de configuration fixe (SANS VARIABLES)
# Assurez-vous que votre fichier infra/docker/nginx.conf contient "listen 8080;"
COPY infra/docker/nginx.conf /etc/nginx/conf.d/default.conf

# Configuration des permissions pour l'utilisateur nginx (sécurité)
# On donne les droits sur les dossiers de cache, logs et config pour éviter les erreurs au démarrage
RUN touch /var/run/nginx.pid && \
    chown -R nginx:nginx /var/run/nginx.pid /var/cache/nginx /var/log/nginx /usr/share/nginx/html /etc/nginx/conf.d/

USER nginx
# Le port interne est désormais fixe
EXPOSE 8080

# On lance nginx directement en ignorant les scripts d'entrypoint qui cherchent des variables
CMD ["nginx", "-g", "daemon off;"]