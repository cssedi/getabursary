---
id: T-049
title: Server bootstrap script and deploy runbook (Hetzner + Cloudflare + Neon)
phase: 6
area: infra
model: haiku
status: todo
depends_on: [T-047]
requirements: [NFR-07]
issue: 95
---

## Goal
The owner can go from a fresh Ubuntu server to a running site by following one document and running one script, without needing to know Linux.

## Context
- `AGENTS.md`, `specs/03-conventions.md`, **`specs/08-hosting.md` §2b–§2c**, `specs/01-architecture.md` §5

## Scope
- `deploy/bootstrap.sh` (new), `docs/DEPLOY.md` (new; T-048 adds its own section later, so leave a heading "Automatic deploys" with "Coming in T-048")

## Requirements
1. `deploy/bootstrap.sh` (bash, `set -euo pipefail`, idempotent, run as root once): apt update/upgrade; install Docker Engine + compose plugin from Docker's apt repo; `unattended-upgrades`; a 2 GB swap file (`vm.swappiness=10`); user `deploy` in the `docker` group with the root account's `authorized_keys`; disable SSH password login; `ufw`: allow 22, allow 80/443 **only from Cloudflare's published IPv4/IPv6 ranges** (fetched from cloudflare.com/ips-v4 and ips-v6); `/opt/getabursary` and `/opt/getabursary/certs` owned by `deploy`; copy `.env.prod.example` to `/opt/getabursary/.env` (mode 600) **only if it doesn't exist**.
2. `docs/DEPLOY.md`, plain-language numbered steps for a non-expert:
   1. Cloudflare: add the domain; at the registrar, change the nameservers; add an **A record** `@` → server IPv4 (proxied, orange cloud) and `www` CNAME → `@` (proxied); SSL/TLS mode **Full (strict)**; create an **Origin Certificate** and save it as `/opt/getabursary/certs/origin.pem` and `origin.key`.
   2. Copy the script up and run it: `scp deploy/bootstrap.sh root@<ip>:` then `ssh root@<ip> bash bootstrap.sh`.
   3. Fill in `/opt/getabursary/.env` with `nano`: where each value comes from (Neon dashboard → **pooled** connection string; Google Cloud console; Brevo SMTP; the extraction API key; `openssl rand -hex 32` for the salt).
   4. First manual deploy: `docker compose --profile tools run --rm migrate` then `docker compose up -d` (from `/opt/getabursary`); how to check logs (`docker compose logs -f api`); how to know it works (open the site; `/api/v1/health`).
   5. Making yourself admin (`Admin__Emails`), then a restart.
   6. Troubleshooting: out of memory (`free -h`, `docker stats`), 502 from Cloudflare, Neon sleeping (the first request is slow).
   Secrets never go in the repo, issues or chat.

## Acceptance criteria
- [ ] `shellcheck deploy/bootstrap.sh` is clean (run via `docker run --rm -v "$PWD:/mnt" koalaman/shellcheck:stable deploy/bootstrap.sh`)
- [ ] Running the script twice changes nothing the second time (explain how you checked, e.g. guards on each step)
- [ ] `docs/DEPLOY.md` covers every step above; Task file `status: done`

## How to verify
```bash
docker run --rm -v "$PWD:/mnt" koalaman/shellcheck:stable deploy/bootstrap.sh
```
