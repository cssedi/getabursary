# Build context is the repo root: docker build -f src/GetABursary.Worker/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY data/seed/ data/seed/
COPY src/GetABursary.Core/ src/GetABursary.Core/
COPY src/GetABursary.Worker/ src/GetABursary.Worker/
RUN dotnet publish src/GetABursary.Worker/GetABursary.Worker.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app .
COPY data/seed/ /app/seed/
ENV DOTNET_gcServer=0 \
    DOTNET_GCConserveMemory=5 \
    Seeding__Path=/app/seed
USER $APP_UID
ENTRYPOINT ["dotnet", "GetABursary.Worker.dll"]
