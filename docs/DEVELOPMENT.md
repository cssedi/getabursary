# Local development

This guide gets a developer (or agent) from a fresh clone to a running local database, and explains how to configure the app without committing secrets.

## Prerequisites

- **.NET 10 SDK** (the version is pinned in `global.json` once the solution exists)
- **Docker** (Docker Desktop, or Docker Engine with the Compose plugin). Docker must be running for the database and for the integration tests.
- **Node 22** (for the Angular web app, `src/web`)

## 1. Create your local environment file

```bash
cp .env.example .env
```

`.env` is git-ignored. Edit it if you want different database values. The defaults work out of the box.

## 2. Start PostgreSQL

```bash
docker compose up -d db
docker compose ps   # the db service should show "healthy" after a few seconds
```

This starts PostgreSQL 17 on `localhost:5433` (not 5432, so it never clashes with another local Postgres), with data kept in the named volume `getabursary-pgdata`. Stop it with:

```bash
docker compose down        # stops the container, keeps data
docker compose down -v     # stops it and deletes the data volume
```

## 3. Configure the connection string with user secrets

The API and the Worker both read `ConnectionStrings:Default`. Set it per project with `dotnet user-secrets` so it stays out of the repository. Run these from the repository root, once the projects exist:

```bash
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5433;Database=getabursary;Username=getabursary;Password=localdev" --project src/GetABursary.Api
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5433;Database=getabursary;Username=getabursary;Password=localdev" --project src/GetABursary.Worker
```

Use the same values as in your `.env` if you changed them. Other settings, such as `Extraction:ApiKey` and `Reports:IpHashSalt` (or `Reports__IpHashSalt` as an environment variable), follow the same pattern: `dotnet user-secrets set "<Key>" "<value>" --project <project>`.

## 4. Run the tests

```bash
dotnet build
dotnet test
```

Integration tests use **Testcontainers**, which starts its own PostgreSQL container. **Docker must be running** before you run `dotnet test`. You do not need `docker compose up` for the tests.

Adapter tests use saved HTML fixtures and make no live network calls.

## Never commit secrets

- Do **not** commit `.env`, real connection strings, API keys, passwords or personal data. This repository is public.
- Keep real values in `.env` (git-ignored) or in `dotnet user-secrets`. Only placeholders belong in `.env.example`.
- Before pushing, check that `git status` shows no `.env` or `appsettings.*.local.json` file.
- If a secret is committed by accident, rotate it immediately. Deleting the commit is not enough.
