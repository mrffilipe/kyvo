# Build from repository root:
#   docker build -f frontend/Dockerfile -t mrffilipe/kyvo-frontend:<tag> .
#
# Optional overrides (separate API/UI hosts):
#   --build-arg VITE_API_BASE_URL=https://api.example.com
#   --build-arg VITE_OAUTH_REDIRECT_URI=https://app.example.com/auth/callback

FROM node:22-alpine AS build
WORKDIR /app

ARG VITE_API_BASE_URL=
ARG VITE_API_VERSION=1.0
ARG VITE_API_TIMEOUT_MS=30000
ARG VITE_OAUTH_CLIENT_ID=platform-admin-web
ARG VITE_OAUTH_REDIRECT_URI=

ENV VITE_API_BASE_URL=$VITE_API_BASE_URL
ENV VITE_API_VERSION=$VITE_API_VERSION
ENV VITE_API_TIMEOUT_MS=$VITE_API_TIMEOUT_MS
ENV VITE_OAUTH_CLIENT_ID=$VITE_OAUTH_CLIENT_ID
ENV VITE_OAUTH_REDIRECT_URI=$VITE_OAUTH_REDIRECT_URI

COPY frontend/package.json frontend/package-lock.json* ./
RUN npm ci

COPY frontend/ .
RUN npm run build

FROM nginx:alpine AS runtime
COPY frontend/nginx/default.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80
