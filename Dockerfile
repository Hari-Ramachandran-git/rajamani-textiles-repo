FROM node:22-alpine AS frontend-build
WORKDIR /src
COPY package.json package-lock.json ./
COPY apps/client/package.json apps/client/package.json
COPY apps/admin/package.json apps/admin/package.json
RUN npm ci
COPY apps/client/ apps/client/
COPY apps/admin/ apps/admin/
COPY packages/ packages/
RUN VITE_API_BASE_URL=/api npm run build -w apps/client
RUN VITE_API_BASE_URL=/api npm run build -w apps/admin -- --base=/admin/

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY apps/api/HariNari.Api.csproj apps/api/
RUN dotnet restore apps/api/HariNari.Api.csproj
COPY apps/api/ apps/api/
RUN dotnet publish apps/api/HariNari.Api.csproj -c Release --no-restore -o /out /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    DataDirectory=/data
COPY --from=build /out/ ./
COPY --from=frontend-build /src/apps/client/dist/ ./wwwroot/
COPY --from=frontend-build /src/apps/admin/dist/ ./wwwroot/admin/
RUN mkdir -p /data
EXPOSE 8080
ENTRYPOINT ["dotnet", "HariNari.Api.dll"]
