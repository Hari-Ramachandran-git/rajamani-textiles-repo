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
RUN mkdir -p /data
EXPOSE 8080
ENTRYPOINT ["dotnet", "HariNari.Api.dll"]
