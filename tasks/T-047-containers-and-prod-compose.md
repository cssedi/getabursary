---
id: T-047
title: Dockerfiles, production compose, Caddy, migrate step and persisted Data Protection keys
phase: 6
area: infra
model: sonnet
status: todo
depends_on: []
requirements: [NFR-07]
issue: 93
---

## Goal
The whole app runs on one small server with `docker compose -f deploy/docker-compose.prod.yml up -d`, pointed at an external Postgres (Neon). Sign-ins survive restarts.

## Context
- `AGENTS.md`, `specs/03-conventions.md`, **`specs/08-hosting.md` §2b–§2c**, `specs/01-architecture.md` §5

## Scope
- `src/GetABursary.Api/Dockerfile`, `src/GetABursary.Worker/Dockerfile`, `src/web/Dockerfile`, `.dockerignore` (repo root; build context is the repo root)
- `deploy/docker-compose.prod.yml`, `deploy/Caddyfile`, `deploy/.env.prod.example` (placeholders only)
- `src/GetABursary.Api/Program.cs` (the `--migrate` mode), `src/GetABursary.Api/Extensions/AuthServiceCollectionExtensions.cs` (Data Protection), `src/GetABursary.Core/Data/**` (DataProtectionKeys DbSet + **one** migration `AddDataProtectionKeys`), `Directory.Packages.props` + the relevant `.csproj` (`Microsoft.AspNetCore.DataProtection.EntityFrameworkCore`)
- `tests/GetABursary.Api.Tests/**` (tests for the above), `.github/workflows/ci.yml` (one job that runs `docker build` for the 3 images, no push)

## Requirements
1. Multi-stage Dockerfiles on official images (`mcr.microsoft.com/dotnet/sdk:10.0` → `aspnet:10.0` / `runtime:10.0`; `node:22-alpine` for web). Non-root user. API listens on 8080, web on 4000. The seed JSON (`data/seed/**`) is copied into the API and Worker images and `Seeding:Path` points to it. Set `DOTNET_gcServer=0` and `DOTNET_GCConserveMemory=5`.
2. `--migrate` argument on the API: apply EF migrations, run `IDataSeeder`, run `AuthSeeder`, exit 0 (non-zero on failure). Normal start is unchanged.
3. Data Protection: `PersistKeysToDbContext<GetABursaryDbContext>()` with `SetApplicationName("getabursary")`, outside Development and in tests.
4. `deploy/docker-compose.prod.yml`: services `migrate` (api image, `command: ["--migrate"]`, `restart: "no"`, profile `tools`), `api`, `worker`, `web` (`API_INTERNAL_URL=http://api:8080`), `caddy` (ports 80/443, mounts `deploy/Caddyfile` and `/opt/getabursary/certs` read-only). Images `ghcr.io/cssedi/getabursary-{api,worker,web}:${IMAGE_TAG:-latest}`. `env_file: /opt/getabursary/.env`. `mem_limit` as in spec 08 §2c, `restart: unless-stopped`, json-file logs capped (10m × 3). No database service.
5. `deploy/Caddyfile`: site `{$SITE_DOMAIN}` and `www.` (www redirects to apex). TLS from `/certs/origin.pem` + `/certs/origin.key` (Cloudflare Origin CA). `/api/*` and `/signin-google*` → `api:8080`; everything else → `web:4000`. Gzip/zstd on. Security headers: HSTS, `X-Content-Type-Options`, `Referrer-Policy: strict-origin-when-cross-origin`.
6. `deploy/.env.prod.example`: every setting the containers read (`ConnectionStrings__Default` with `SSL Mode=Require`, `App__PublicBaseUrl`, `Admin__Emails`, `Auth__Google__*`, `Email__Smtp__*`, `Extraction__*`, `Reports__IpHashSalt`, `ASPNETCORE_ENVIRONMENT=Production`, `SITE_DOMAIN`), placeholder values only, with a comment for each.

## Acceptance criteria
- [ ] `docker build` succeeds for all 3 images in CI
- [ ] Test: `--migrate` against a Testcontainers Postgres creates the schema and seed, then exits 0
- [ ] Test: Data Protection keys are written to the DB (a cookie protected by one app instance is readable by a second instance on the same DB)
- [ ] `docker compose -f deploy/docker-compose.prod.yml config` is valid (with the example env)
- [ ] No secrets anywhere; `dotnet build` and `dotnet test` pass; Task file `status: done`

## How to verify
```bash
docker build -f src/GetABursary.Api/Dockerfile . && docker build -f src/GetABursary.Worker/Dockerfile . && docker build -f src/web/Dockerfile .
dotnet test
```
