# Build context is the repo root: docker build -f src/GetABursary.Api/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY data/seed/ data/seed/
COPY src/GetABursary.Core/ src/GetABursary.Core/
COPY src/GetABursary.Api/ src/GetABursary.Api/
RUN dotnet publish src/GetABursary.Api/GetABursary.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
COPY data/seed/ /app/seed/
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_gcServer=0 \
    DOTNET_GCConserveMemory=5 \
    Seeding__Path=/app/seed
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "GetABursary.Api.dll"]
