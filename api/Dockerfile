# The Wordrise API. Build from the api/ folder:  docker build -t wordrise-api api

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# The solution-wide settings first, so restore stays cached until a package changes.
COPY Directory.Build.props Directory.Packages.props nuget.config ./
COPY src/ src/

RUN dotnet publish src/Wordrise.Host/Wordrise.Host.csproj \
    --configuration Release \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Where the app keeps what must survive a restart (mount volumes here):
#   /data/media  lesson recordings        /data/keys  sign-in encryption keys
RUN mkdir -p /data/media /data/keys && chown -R app:app /data

COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_EnableDiagnostics=0 \
    Media__Storage__Path=/data/media \
    DataProtection__KeysPath=/data/keys

USER app
EXPOSE 8080

# The image has no curl: a plain TCP request is enough to ask /health.
HEALTHCHECK --interval=30s --timeout=5s --start-period=40s --retries=3 \
    CMD bash -c 'exec 3<>/dev/tcp/127.0.0.1/8080 && printf "GET /health HTTP/1.0\r\n\r\n" >&3 && head -n1 <&3 | grep -q " 200 "'

ENTRYPOINT ["dotnet", "Wordrise.Host.dll"]
