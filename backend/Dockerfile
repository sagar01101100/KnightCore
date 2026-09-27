FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY KnightCore.Api/KnightCore.Api.csproj KnightCore.Api/
RUN dotnet restore KnightCore.Api
COPY KnightCore.Api/ KnightCore.Api/
RUN dotnet publish KnightCore.Api -c Release -o /out --no-restore
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "KnightCore.Api.dll"]
