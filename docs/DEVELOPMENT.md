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

## Visual check

Once: `cd src/web && npm ci && npx playwright install chromium`. Then, from the repo root: `node scripts/visual-check.mjs <task-id> / /search` (add `--sign-in-admin` for signed-in pages).
It starts the db, API (port 5266) and web (4200), loads `data/dev/sample-bursaries.sql` (15 bursaries; safe to re-run), and saves `docs/screenshots/<task-id>/<page>-{mobile,desktop}.png` (375×812, 1440×900).
The dev-only admin is `admin@getabursary.local` / `DevOnly-Passw0rd!`. Open the PNGs before review.

## Never commit secrets

- Do **not** commit `.env`, real connection strings, API keys, passwords or personal data. This repository is public.
### Google sign-in (optional)

Without credentials the app still starts and `GET /api/v1/auth/external/google` returns 404.

1. In the [Google Cloud Console](https://console.cloud.google.com/), create or pick a project, then open **APIs & Services > OAuth consent screen** and configure it (External, add your own Google account as a test user).
2. Open **APIs & Services > Credentials > Create credentials > OAuth client ID**, application type **Web application**.
3. Add the authorised redirect URI `{App:PublicBaseUrl}/signin-google`, for example `https://localhost:5001/signin-google` locally.
4. Store the client ID and secret with user secrets (never commit them):

```bash
dotnet user-secrets set "Auth:Google:ClientId" "<client id>" --project src/GetABursary.Api
dotnet user-secrets set "Auth:Google:ClientSecret" "<client secret>" --project src/GetABursary.Api
```

- Keep real values in `.env` (git-ignored) or in `dotnet user-secrets`. Only placeholders belong in `.env.example`.
- Before pushing, check that `git status` shows no `.env` or `appsettings.*.local.json` file.
- If a secret is committed by accident, rotate it immediately. Deleting the commit is not enough.
