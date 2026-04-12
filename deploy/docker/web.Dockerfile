FROM node:20-bookworm-slim AS build
WORKDIR /src

COPY . /src/system/integration-plataform
COPY --from=archon-ui . /src/frameworks/archon-ui

WORKDIR /src/system/integration-plataform/IntegrationPlataform/IntegrationPlataform.Web

ARG VITE_API_BASE_URL=http://69.62.96.209:8081/api
ARG VITE_IDENTITY_PROVIDER_WEB=http://69.62.96.209/
ARG VITE_IDENTITY_PROVIDER_API=http://69.62.96.209/api
ENV VITE_API_BASE_URL=${VITE_API_BASE_URL}
ENV VITE_IDENTITY_PROVIDER_WEB=${VITE_IDENTITY_PROVIDER_WEB}
ENV VITE_IDENTITY_PROVIDER_API=${VITE_IDENTITY_PROVIDER_API}

RUN npm ci
RUN npm run build

FROM nginx:1.27-alpine AS runtime
COPY deploy/nginx/web.nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /src/system/integration-plataform/IntegrationPlataform/IntegrationPlataform.Web/dist /usr/share/nginx/html

EXPOSE 80
