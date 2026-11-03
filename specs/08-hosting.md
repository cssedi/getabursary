# 08 — Hosting cost analysis (Epic C)

> Status: **Draft v0.1** (2026-11-02). Prices checked 2026-11-02. Rates used: **€1 ≈ R18.65**, **$1 ≈ R16.64**. Amounts exclude VAT unless stated. Foreign providers may add 15% SA VAT on digital services.

## 1. What we have to run
One small server runs everything (spec 01 §4, "one VPS with Docker Compose + Caddy"):

| Container | Rough RAM |
|---|---|
| Postgres 17 | 300–600 MB |
| API (.NET 10) | 150–250 MB |
| Worker (.NET 10, nightly scrape) | 100–200 MB, busy only around 03:00 |
| Web (Angular SSR, Node) | 100–200 MB |
| Caddy (TLS, reverse proxy) | 30 MB |

**About 1–1.3 GB in use, so 4 GB RAM / 2 vCPU leaves room for traffic spikes in the bursary season (Aug–Oct).** 2 GB would work but is tight while the scrape runs.

## 2. Recommended: Hetzner CX23 (Germany or Finland) + Cloudflare free

| Item | Choice | Per month | Rand per month |
|---|---|---|---|
| Server | Hetzner **CX23**: 2 vCPU, 4 GB, 40 GB SSD, 20 TB traffic | €5.49 | R102 |
| Public IPv4 | Hetzner primary IPv4 | €0.50 | R9 |
| Server backups | Hetzner daily backups (~20% of server) | ~€1.10 | R21 |
| Off-site DB backup | nightly `pg_dump` to Cloudflare R2 (10 GB free) | 0 | R0 |
| CDN, DNS, DDoS, TLS at edge | Cloudflare Free | 0 | R0 |
| Email (confirm, reset) | Brevo free (300/day) via SMTP (`Email:Smtp:*`) | 0 | R0 |
| Eligibility extraction | Claude Haiku 5.5 (see §4) | ~$0.30 | ~R5 |
| Uptime alerts | UptimeRobot / Better Stack free | 0 | R0 |
| Domain | getabursary.co.za, ~R110/year | – | R9 |
| **Total** | | | **≈ R146/month (≈ R1,750/year)** |

Add 15% if VAT is charged: about **R168/month**.

**Why this one:** cheapest 4 GB option from a reliable provider, with backups and 20 TB of traffic included. Cloudflare caches static files and SSR pages close to SA users, which hides most of the Europe → SA delay (~160–180 ms), so pages still feel quick.

## 2b. Current choice: test setup (owner, 2026-11-02: "one server, test traffic, not prod yet")

| Item | Choice | Rand per month |
|---|---|---|
| Server | 1× Hetzner CX23 + IPv4 (web, API, worker, Caddy). **No Hetzner backups**: there's no data on it | ≈ R111 |
| Database | **Neon free**, Frankfurt (AWS eu-central-1, a few ms from Hetzner DE). Supports `pg_trgm` | **R0** |
| Cloudflare, Brevo, uptime | free | R0 |
| Domain + LLM | | ≈ R14 |
| **Total** | | **≈ R125** |

- **Why Neon over Postgres on the same server:** the data survives a server rebuild, there's nothing to patch or back up, and you can make a branch (a full copy) to test migrations.
- **Limits (free):** 1 project and small storage (sources say 0.5–3 GB; we need well under 100 MB). The DB **sleeps after 5 min idle**, so the first request after a quiet spell takes about half a second longer. The nightly scrape wakes it.
- **Config:** `ConnectionStrings:Default` gets `SSL Mode=Require` and the Neon *pooled* host. The connection string is a secret in the server's `.env`, never in the repo.
- **Fallback:** Supabase free (500 MB, Frankfurt, pauses after 1 week with no traffic).
- **Going to prod:** move to §2a tier 2 (or Neon's paid plan). Moving is a `pg_dump`/`pg_restore` plus a connection-string change.

Sources: [Neon pg_trgm](https://neon.com/docs/extensions/pg_trgm), [free Postgres tiers 2026 (Koyeb)](https://www.koyeb.com/blog/top-postgresql-database-free-tiers-in-2026), [selfhost.dev comparison](https://selfhost.dev/managed-postgresql-comparison-2026/).

## 2c. Test deployment design (decided 2026-11-03)
Owner's setup: a **Hetzner Falkenstein** shared-vCPU x86 server (small: assume **1 vCPU / 1–2 GB RAM / 20 GB**), **Neon free in AWS eu-central-1 (Frankfurt)**, domain at a SA registrar with DNS on **Cloudflare (proxied)**.

```
Browser ─▶ Cloudflare (proxy, TLS) ─▶ Caddy :443 (Cloudflare Origin CA cert)
                                        ├─ /api/*, /signin-google ─▶ api:8080
                                        └─ everything else       ─▶ web:4000 (SSR; API_INTERNAL_URL=http://api:8080)
worker (nightly scrape) ─┐
api ─────────────────────┴─▶ Neon (SSL, pooled host)
```
- **Images** are built in GitHub Actions (linux/amd64) and pushed to GHCR, never built on the server (1 GB isn't enough). Tags: the commit sha, plus `latest`.
- **Migrations:** a one-shot `migrate` service (the API image run with `--migrate`) applies EF migrations and the reference seed, then exits. The deploy runs it before `up -d`.
- **Memory:** 2 GB swap file; `.NET` workstation GC + `DOTNET_GCConserveMemory`; compose `mem_limit`s (api 300m, worker 300m, web 256m, caddy 64m).
- **Data Protection keys** are stored in Postgres, so sign-in cookies survive container restarts and redeploys.
- **Secrets** live only in `/opt/getabursary/.env` on the server (mode 600) and in GitHub Actions secrets for SSH. Nothing secret goes in the repo or the images (the images are public on GHCR because the repo is public).
- **Firewall:** 22 open; 80/443 only from Cloudflare IP ranges.
- **Deploy gate:** `vars.DEPLOY == 'on'` (the owner sets it after the first manual deploy works).

## 2a. Availability tiers (owner: "can cost more, I want some availability")

| Tier | Setup | If a server dies | Rand per month |
|---|---|---|---|
| 1. Single server (§2) | 1× CX23, daily backups | Down until restored, about 30–60 min | ≈ R146 |
| **2. Recommended** | Hetzner **LB11** load balancer + **2× CX23** app servers (web + API on both; worker on one) + **managed Postgres** (DigitalOcean 1 node, daily backups + point-in-time restore) + Cloudflare cache | App keeps running. A DB failure means a few minutes while the provider replaces the node. Deploys don't take the site down (one server at a time) | ≈ R101 + R223 + R250 + R23 ≈ **R600** |
| 3. Season mode | Tier 2, with DB upgraded to a primary + standby (automatic failover) | Survives an app or DB server failure in seconds | ≈ R101 + R223 + R998 + R23 ≈ **R1,350** |
| 4. AWS Cape Town | 2× EC2 + ALB + RDS Multi-AZ, af-south-1 | Same as tier 3, hosted in SA | ≈ R2,000–2,500 (estimate) |

**Plan:** launch on **tier 2**, switch the DB to tier 3 for the bursary season (Aug–Oct) if traffic justifies it, then back again. The resize is one setting with no code change.

**Cheap extra:** in Cloudflare, cache the public search and detail pages for a few minutes, so students can still browse when the origin has a blip.

**What running 2 app servers needs in code** (goes in the Phase 6 deploy tasks):
- ASP.NET **Data Protection keys must be shared** (`PersistKeysToDbContext`). Otherwise a sign-in cookie made on server A is rejected by server B.
- Only **one worker** runs (`Worker:Enabled=true` on one server), so a scrape never runs twice.
- Migrations run once per deploy step, not on both servers at startup.
- Rate limits are per server (acceptable).

Prices: [Hetzner LB11 €5.39](https://cloudpricecheck-com.pages.dev/hetzner/hetzner-load-balancer-pricing), [DigitalOcean Postgres $15 single / $60 with standby](https://docs.digitalocean.com/products/databases/postgresql/details/pricing/), [RDS db.t4g.small af-south-1 ~$30 single-AZ](https://www.bytebase.com/dbcost/rds/instance/db.t4g.small.md).

## 3. Alternatives

| Option | Specs | Rand per month | Trade-off |
|---|---|---|---|
| Hetzner CAX11 (ARM) | 2 vCPU, 4 GB | ~R121 + backups | .NET and Postgres run fine on ARM; same price band |
| Hetzner CX33 | 4 vCPU, 8 GB | ~R168 + backups | Headroom for year two |
| SA-hosted VPS (e.g. ZappieHost Johannesburg, 4 GB) | 6 vCPU, 4 GB | ~R316 ($19) | Lowest latency, POPIA story is simpler; roughly 2× the price |
| AWS af-south-1 (Cape Town), t4g.medium + RDS | per brief §9 | R1,000+ | Managed DB and scaling; we don't need it at MVP traffic |

**Risk:** in October 2026, checks reported Hetzner's shared-vCPU plans as temporarily "unavailable" to order. Order one as soon as Phase 5 lands. If none is available, use the CAX line or an SA-hosted VPS; the Docker Compose setup is the same on all of them.

## 4. LLM extraction cost
- Haiku 5.5: $0.10 per million input tokens and $0.50 per million output tokens (Batch API halves that).
- One listing is about 3,000 tokens in and 500 out, so about **$0.00055 per listing**.
- Only new or changed pages are sent (FR-25 snapshot hash). At ~500 per month that's **≈ $0.28 (R5)**. A full re-extract of 5,000 listings costs about **$2.75 (R46)** once.

## 5. One-off and yearly costs

| Item | Cost |
|---|---|
| getabursary.co.za registration | ~R100–120 per year (shop SA registrars; renewals vary) |
| Google OAuth client | free |
| Google AdSense (ads, FR revenue) | free; earns, doesn't cost |
| Privacy policy / POPIA info officer registration | free (registration with the Information Regulator) |

## 6. When to move up
- **RAM over 80%** or **p95 API latency over 500 ms** in season → CX33 (resize in minutes, ~+R60/month).
- **More than ~50k monthly users**, or the database is over 20 GB → split Postgres onto its own server or a managed DB.

## 7. Phase 6 tasks this implies (not yet written)
0. Shared Data Protection keys in Postgres; `Worker:Enabled` flag; migrations as a separate deploy step (tier 2).
1. `docker-compose.prod.yml` + Caddyfile + `.env.example` for prod (no secrets in repo).
2. GitHub Action: build images → GHCR → deploy over SSH (secrets in repo settings, set by the owner).
3. Nightly `pg_dump` → Cloudflare R2, with a restore test.
4. Uptime check + Hetzner usage alert.

## Sources
- Hetzner prices after the 15 June 2026 increase: [Private DevOps](https://privatedevops.com/news/hetzner-june-2026-cloud-price-increase-what-to-do), [Bitdoze](https://www.bitdoze.com/md/hetzner-cloud-cost-optimized-plans.md)
- Claude pricing: [platform.claude.com](https://platform.claude.com/docs/en/about-claude/pricing)
- .co.za pricing: [domainoffer.net](https://domainoffer.net/price-compare-co.za)
- SA VPS: [ZappieHost](https://zappiehost.com/south-africa-vps)
- Brevo free SMTP: [brevo.com](https://www.brevo.com/free-smtp-server)
- Rates: [USD/ZAR](https://www.poundsterlinglive.com/history/ZAR-USD-2026), [EUR/ZAR](https://www.poundsterlinglive.com/history/EUR-ZAR-2026)
