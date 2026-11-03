---
id: T-048
title: Deploy workflow - build images to GHCR and roll out to the server over SSH
phase: 6
area: infra
model: sonnet
status: done
depends_on: [T-047]
requirements: [NFR-07]
issue: 94
---

## Goal
Every green commit on main is built into images and deployed to the test server automatically (once the owner switches it on), with migrations run first.

## Context
- `AGENTS.md`, `specs/03-conventions.md`, **`specs/08-hosting.md` §2b–§2c**, `specs/01-architecture.md` §5

## Scope
- `.github/workflows/deploy.yml` (new), `docs/DEPLOY.md` (the "Automatic deploys" section only)

## Requirements
1. Triggers: `workflow_run` of CI on `main` with conclusion success, and `workflow_dispatch` (input `image_tag`, optional). The deploy job only runs when `vars.DEPLOY == 'on'` (the build job always runs on main).
2. Build job: `docker/build-push-action` for api, worker, web (linux/amd64), pushed to `ghcr.io/cssedi/getabursary-{api,worker,web}` with tags `${{ github.sha }}` and `latest`, using GHA cache. `permissions: packages: write`.
3. Deploy job (`environment: test`, `concurrency: deploy-test`, no cancel): SSH using secrets `DEPLOY_HOST`, `DEPLOY_USER`, `DEPLOY_SSH_KEY`, and `DEPLOY_KNOWN_HOSTS` (strict host checking; no `StrictHostKeyChecking=no`). Copy `deploy/docker-compose.prod.yml` and `deploy/Caddyfile` to `/opt/getabursary/`. Then run there: `IMAGE_TAG=<sha> docker compose pull` → `docker compose --profile tools run --rm migrate` (stop if it fails) → `docker compose up -d --remove-orphans` → `docker image prune -af --filter "until=72h"`.
4. Smoke check: after up, `curl -fsS https://$SITE/api/v1/health` (from the runner, retried for 60 s, `SITE` from `vars.SITE_DOMAIN`). Failure fails the job.
5. No secret is echoed; use `ssh-agent` or a temp key file with `chmod 600` that is deleted at the end.

## Acceptance criteria
- [ ] `actionlint` passes on the workflow (run it with `npx` or the docker image; mention the result in the PR)
- [ ] The workflow skips the deploy job when `vars.DEPLOY` isn't `on`
- [ ] `docs/DEPLOY.md` lists the 4 secrets and 2 variables (`DEPLOY`, `SITE_DOMAIN`) and how to roll back (`workflow_dispatch` with an older sha)
- [ ] Task file `status: done`

## How to verify
```bash
npx --yes actionlint .github/workflows/deploy.yml || docker run --rm -v "$PWD:/repo" -w /repo rhysd/actionlint:latest
```

## Done by the orchestrator (2026-11-03)
Agent tokens can't push workflow files, so the orchestrator wrote `.github/workflows/deploy.yml` directly. T-049 documents it in `docs/DEPLOY.md`.
