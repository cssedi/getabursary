# Deploying GetABursary (test server)

GitHub does the deploying. You set things up once (about 10 minutes), then every change merged to `main` goes live by itself.

What GitHub does on each deploy (`.github/workflows/deploy.yml`):
1. Builds the app (API, scraper, website) and stores the images privately on GitHub.
2. Prepares the server the first time: Docker, firewall (web traffic only from Cloudflare), swap, security updates (`deploy/bootstrap.sh`).
3. Writes the server's settings file from your GitHub secrets.
4. Updates the database, starts the app, and checks that `https://<your domain>/api/v1/health` answers.

You need: the Hetzner server (with your SSH key), the domain on Cloudflare with the 2 **A** records (`@` and `www`, both proxied, pointing at the server's IPv4), and the Neon project.

Secrets never go in the repo, issues or chat, only into GitHub's secret settings.

## 1. Cloudflare: one setting

**SSL/TLS → Overview → Configure** → choose **Full** (not "Flexible", not "Full (strict)"). Save.

The server makes its own certificate, and "Full" is the mode that accepts it.

## 2. Give GitHub a key to your server

On your Mac, in Terminal:

```bash
ssh-keygen -t ed25519 -f ~/.ssh/getabursary_deploy -N ""
ssh-copy-id -i ~/.ssh/getabursary_deploy.pub root@<server-ip>
```

The second command logs in with your normal key and adds the new one. Check it:

```bash
ssh -i ~/.ssh/getabursary_deploy root@<server-ip> echo ok
```

It should print `ok`.

## 3. Add the secrets and variables on GitHub

GitHub → the repo → **Settings → Secrets and variables → Actions**.

**Secrets** tab → **New repository secret**, one at a time:

| Name | Value |
|---|---|
| `DEPLOY_HOST` | The server's IPv4 address. |
| `DEPLOY_SSH_KEY` | Run `cat ~/.ssh/getabursary_deploy` and paste **everything**, including the `-----BEGIN` and `-----END` lines. |
| `NEON_DATABASE_URL` | Neon → your project → **Connect** → copy the connection string that starts with `postgresql://`. Pooled or not doesn't matter: the deploy uses the direct host automatically. |
| `ADMIN_EMAILS` | Your email address. |
| `ADMIN_PASSWORD` | A password for your admin account (8+ characters, no single quote `'`). The first deploy creates the account with it. Changing this secret later does nothing to an existing account. |

**Variables** tab → **New repository variable**:

| Name | Value |
|---|---|
| `SITE_DOMAIN` | `getabursary.co.za` |
| `DEPLOY` | `on` |

## 4. Deploy

GitHub → **Actions → Deploy → Run workflow** (leave the box empty) → **Run workflow**.

The first run takes about 10 minutes, because it sets up the server. When it's green, open `https://getabursary.co.za` and sign in with your admin email and password. The account menu shows **Admin**.

From now on, every merge to `main` deploys by itself.

## Optional secrets (add any time, then run the Deploy workflow again)

| Name | What it turns on |
|---|---|
| `EXTRACTION_API_KEY` | Real AI eligibility extraction for scraped bursaries (an Anthropic API key). Without it, extraction is `fake`. |
| `SMTP_HOST`, `SMTP_USER`, `SMTP_PASSWORD`, `SMTP_FROM` | Real emails (Brevo → SMTP & API, port 587). Without them, new accounts don't need email confirmation, and email links go to the server log. |
| `GOOGLE_CLIENT_ID`, `GOOGLE_CLIENT_SECRET` | "Continue with Google". Google Cloud → Credentials → OAuth client (Web application), redirect URI `https://getabursary.co.za/signin-google`. |
| `DEPLOY_KNOWN_HOSTS` | Pins the server's identity (`ssh-keyscan <server-ip>` output). Without it, GitHub trusts the server the first time it connects on each run. |

## When something goes wrong

The failed step in **Actions → Deploy** says what's wrong. Common ones:

- **"Missing settings"**: a secret or variable from step 3 is missing or misspelled.
- **"Permission denied (publickey)"**: `DEPLOY_SSH_KEY` isn't the whole private key, or step 2 didn't finish.
- **Health check failed**: look at the API's log:
  ```bash
  ssh root@<server-ip> docker compose -f /opt/getabursary/docker-compose.prod.yml logs --tail 100 api
  ```
- **Cloudflare error 525 or 526**: the SSL mode isn't **Full** (step 1).
- **Too many redirects**: the SSL mode is "Flexible". Set it to **Full**.
- **Slow first page after a quiet spell**: expected. Neon's free database sleeps after 5 minutes and takes about half a second to wake.

**Roll back:** Actions → Deploy → Run workflow, and enter an older commit sha in `image_tag`.

**Look around on the server** (optional): `ssh root@<server-ip>`, then `cd /opt/getabursary && docker compose -f docker-compose.prod.yml ps`.
