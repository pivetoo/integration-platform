FROM node:20-bookworm-slim AS build
WORKDIR /src

COPY . /src/system/integration-plataform
COPY --from=archon-ui . /src/frameworks/archon-ui

WORKDIR /src/system/integration-plataform/IntegrationPlataform/IntegrationPlataform.Web

ARG VITE_API_BASE_URL=https://integrationplatform.mainstay.com.br/api
ARG VITE_IDENTITY_PROVIDER_WEB=https://auth.mainstay.com.br
ARG VITE_IDENTITY_PROVIDER_API=https://auth.mainstay.com.br/api
ENV VITE_API_BASE_URL=${VITE_API_BASE_URL}
ENV VITE_IDENTITY_PROVIDER_WEB=${VITE_IDENTITY_PROVIDER_WEB}
ENV VITE_IDENTITY_PROVIDER_API=${VITE_IDENTITY_PROVIDER_API}

RUN npm ci
RUN npm run build

FROM nginx:1.27-alpine AS runtime
COPY deploy/nginx/web.nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /src/system/integration-plataform/IntegrationPlataform/IntegrationPlataform.Web/dist /usr/share/nginx/html

EXPOSE 80
