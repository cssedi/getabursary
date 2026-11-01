---
id: T-002
title: Local dev environment (Docker Compose Postgres + env example + dev guide)
phase: 0
area: infra
model: haiku
status: todo
depends_on: []
requirements: [NFR-07]
issue: null
---

## Goal
A developer (or agent) can start a local PostgreSQL 17 database with one command and knows how to configure the app without committing secrets.

## Context
- `AGENTS.md`, `specs/03-conventions.md` §7
- `specs/01-architecture.md` §5 (configuration & secrets)

## Scope
You may create or modify **only**:
- `docker-compose.yml`
- `.env.example`
- `docs/DEVELOPMENT.md`

## Requirements
1. `docker-compose.yml` with a service named **`db`**:
   - image `postgres:17-alpine`, container name `getabursary-db`
   - env `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD` read from `.env`, with defaults (`getabursary`, `getabursary`, `localdev`) via `${VAR:-default}`
   - port `5432:5432`, named volume `getabursary-pgdata`
   - healthcheck: `pg_isready -U $${POSTGRES_USER}` every 5s
2. `.env.example` with placeholder values for: `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD`, `ConnectionStrings__Default`, `Extraction__ApiKey` (value `replace-me`), `Reports__IpHashSalt` (value `replace-me`).
3. `docs/DEVELOPMENT.md`: prerequisites (.NET 10 SDK, Docker, Node 22), `cp .env.example .env`, `docker compose up -d db`, how to set the connection string with `dotnet user-secrets` for the Api and Worker projects, how to run tests (Testcontainers needs Docker running), and a "never commit secrets" warning.

## Acceptance criteria
- [ ] `docker compose config` validates
- [ ] `docker compose up -d db` starts and becomes healthy
- [ ] `.env` is ignored by git (already in `.gitignore`; don't change it)
- [ ] No real secrets anywhere
- [ ] Task file `status: done`

## How to verify
```bash
cp .env.example .env
docker compose up -d db
docker compose ps   # db is "healthy"
docker compose down
```
