FROM node:22-alpine AS build
WORKDIR /src
COPY package.json package-lock.json ./
COPY apps/client/package.json apps/client/package.json
COPY apps/admin/package.json apps/admin/package.json
RUN npm ci
COPY apps/client/ apps/client/
COPY apps/admin/ apps/admin/
COPY packages/ packages/
ARG VITE_API_BASE_URL
RUN test -n "$VITE_API_BASE_URL" || (echo "Set VITE_API_BASE_URL to your backend's public URL, including /api" >&2; exit 1)
RUN VITE_API_BASE_URL="$VITE_API_BASE_URL" npm run build -w apps/admin

FROM nginx:stable-alpine AS runtime
ENV PORT=8080 NGINX_ENVSUBST_FILTER=^PORT$
COPY deploy/nginx.conf.template /etc/nginx/templates/default.conf.template
COPY --from=build /src/apps/admin/dist/ /usr/share/nginx/html/
EXPOSE 8080
