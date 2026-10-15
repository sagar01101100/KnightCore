FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY KnightCore.Api/KnightCore.Api.csproj KnightCore.Api/
RUN dotnet restore KnightCore.Api
COPY KnightCore.Api/ KnightCore.Api/
RUN dotnet publish KnightCore.Api -c Release -o /out --no-restore
FROM mcr.microsoft.com/dotnet/aspnet:10.0
USER root
# Trust the official AWS RDS CA roots; keep server certificate verification enabled.
RUN apt-get update && apt-get install -y --no-install-recommends ca-certificates curl \
    && curl --fail --silent --show-error https://truststore.pki.rds.amazonaws.com/global/global-bundle.pem -o /tmp/rds-ca.pem \
    && awk ' /-----BEGIN CERTIFICATE-----/ { n++; file=sprintf("/usr/local/share/ca-certificates/aws-rds-%d.crt",n) } file { print > file } /-----END CERTIFICATE-----/ { close(file); file="" } ' /tmp/rds-ca.pem \
    && update-ca-certificates \
    && rm -rf /var/lib/apt/lists/* /tmp/rds-ca.pem
WORKDIR /app
COPY --from=build /out .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "KnightCore.Api.dll"]
