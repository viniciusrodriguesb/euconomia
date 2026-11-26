# syntax=docker/dockerfile:1
FROM node:22.23.5-alpine AS frontend
WORKDIR /web
COPY src/Web/package.json src/Web/package-lock.json ./
RUN --mount=type=cache,target=/root/.npm npm ci
COPY src/Web/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0.404 AS backend
WORKDIR /src
COPY global.json dotnet-tools.json ./
COPY src/Dominio/Dominio.csproj src/Dominio/
COPY src/Aplicacao/Aplicacao.csproj src/Aplicacao/
COPY src/Infraestrutura/Infraestrutura.csproj src/Infraestrutura/
COPY src/Api/Api.csproj src/Api/
RUN --mount=type=cache,target=/root/.nuget/packages dotnet restore src/Api/Api.csproj
COPY src/Dominio/ src/Dominio/
COPY src/Aplicacao/ src/Aplicacao/
COPY src/Infraestrutura/ src/Infraestrutura/
COPY src/Api/ src/Api/
COPY --from=frontend /web/dist/euconomia/browser/ src/Web/dist/euconomia/browser/
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish src/Api/Api.csproj -c Release --no-restore -o /publicacao -p:UseAppHost=false
RUN --mount=type=cache,target=/root/.nuget/packages \
    mkdir -p /migracoes && \
    dotnet restore src/Infraestrutura/Infraestrutura.csproj --runtime linux-x64 && \
    dotnet tool restore && \
    dotnet ef migrations bundle --project src/Infraestrutura --configuration Release \
    --runtime linux-x64 --output /migracoes/efbundle

FROM mcr.microsoft.com/dotnet/aspnet:10.0.15 AS runtime
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=backend /publicacao/ ./
COPY --from=backend --chmod=0555 /migracoes/efbundle ./efbundle
RUN mkdir -p /app/chaves && chown "$APP_UID:$APP_UID" /app/chaves
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    Seguranca__DiretorioChaves=/app/chaves
USER $APP_UID
EXPOSE 8080
HEALTHCHECK --interval=15s --timeout=5s --start-period=20s --retries=5 \
    CMD curl --fail --silent --show-error --header "Host: ${AllowedHosts:-localhost}" http://127.0.0.1:8080/health/ready || exit 1
ENTRYPOINT ["dotnet", "Api.dll"]
