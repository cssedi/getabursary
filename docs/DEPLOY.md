# Deploying GetABursary (test server)

This is the test setup from `specs/08-hosting.md` §2b–§2c: one Hetzner server running the site, Neon as the database, Cloudflare in front, and GitHub Actions doing the builds. You don't need to know Linux. Copy the commands exactly, and replace `<ip>` with your server's IPv4 address and `example.co.za` with your domain.

Before you start you need: a Hetzner server (Ubuntu 24.04 or newer) with your SSH key added when you create it, a domain, a free Cloudflare account, and a free Neon project in Frankfurt.

Keep secrets out of the repo, issues and chat. Passwords, keys and connection strings live only on the server's `.env`, in the password manager you use, and in GitHub's secrets.

## Checklist

| # | What | Where | Done when |
|---|---|---|---|
| 1 | DNS records + **Full (strict)** + Origin certificate | Cloudflare | You have the certificate and key text |
| 2 | Run the setup script | Your Mac → server | It prints "Done" |
| 3 | Save the certificate on the server | Server | 2 files in `/opt/getabursary/certs` |
| 4 | Fill in `.env` (Neon **direct** connection, domain, your email, salt) | Server | No `changeme` left |
| 5 | First manual deploy | Server | `https://<domain>/api/v1/health` answers |
| 6 | Sign up, confirm via the log, become admin | Browser + server | You see "Admin" in the account menu |
| 7 | Deploy key + GitHub secrets/variables + public packages | Mac + GitHub | The next merge deploys by itself |

Not needed yet: mail (Brevo), Google sign-in, the Anthropic key. Leave those settings empty, or `fake` for extraction.

## 1. Cloudflare: point the domain at the server

1. In Cloudflare, choose **Add a site** and enter your domain. Pick the **Free** plan.
2. Cloudflare shows you two nameservers. At your domain registrar (where you bought the domain), replace the existing nameservers with the two Cloudflare ones. Wait until Cloudflare says the site is active (this can take up to a few hours).
3. In Cloudflare, go to **DNS → Records** and add:
   - Type **A**, name `@`, IPv4 address `<ip>`, proxy status **Proxied** (orange cloud).
   - Type **CNAME**, name `www`, target `@`, proxy status **Proxied** (orange cloud).
4. Go to **SSL/TLS → Overview** and set the mode to **Full (strict)**.
5. Go to **SSL/TLS → Origin Server → Create Certificate**. Keep the defaults, add `example.co.za` and `*.example.co.za` as hostnames, and click **Create**. Cloudflare shows two boxes of text, the certificate and the private key. You will paste them into the server in step 2b below. Cloudflare shows the private key only once, so keep this page open until you have saved it.

## 2. Set up the server

### 2a. Copy the setup script to the server and run it

On your Mac, in the repo folder, run:

```bash
scp deploy/bootstrap.sh root@<ip>:
ssh root@<ip> bash bootstrap.sh
```

The first time, `ssh` asks whether to trust the server. Type `yes`. The script takes a few minutes. It installs Docker, sets up automatic security updates, adds a 2 GB swap file, creates a `deploy` user, turns off SSH password login, and sets up the firewall so the website only accepts traffic from Cloudflare. It ends with "Done". It is safe to run again if something stops halfway.

Before you log out, open a second terminal and check that you can still log in as root. Only close the first one when the second works.

### 2b. Save the Cloudflare certificate on the server

Log in to the server (`ssh root@<ip>`) and create the two files. Paste the certificate from step 1.5 into the first file, and the private key into the second:

```bash
nano /opt/getabursary/certs/origin.pem
nano /opt/getabursary/certs/origin.key
```

In `nano`, paste with the right mouse button or **Ctrl+Shift+V**, then press **Ctrl+O**, **Enter** to save, and **Ctrl+X** to exit. Then make the key private:

```bash
chmod 600 /opt/getabursary/certs/origin.key
```

### 2c. Give the server a Neon database connection

Do this step in your browser first, then fill in the file in step 3.

1. Sign in at neon.tech and open your project.
2. Click **Connect**. **Turn Connection pooling off**. The pooled host has `-pooler` in its name, and this app must not use it. Migrations need a session lock that the pooler can't hold.
3. Copy the host, database name, user and password from the connection string. Keep the `SSL Mode=Require` part.

## 3. Fill in the settings file

On the server, open the settings file:

```bash
nano /opt/getabursary/.env
```

It already has every setting, with placeholder values such as `changeme` and `example.co.za`. Replace each placeholder:

| Setting | Where the value comes from |
|---|---|
| `SITE_DOMAIN` | Your domain without `www`, for example `getabursary.co.za`. |
| `ConnectionStrings__Default` | The Neon connection from step 2c. Use the **direct** host (no `-pooler` in it). Keep `SSL Mode=Require`. |
| `App__PublicBaseUrl` | `https://` plus your domain, for example `https://getabursary.co.za`. |
| `Admin__Emails` | Your own email address. See step 5. |
| `Auth__Google__ClientId` and `Auth__Google__ClientSecret` | **Leave empty for now** (Google sign-in stays off). Later: Google Cloud console → APIs & Services → Credentials → OAuth client ID (type **Web application**). Add `https://<your domain>/signin-google` as an authorised redirect URI. Leave both empty to turn off Google sign-in. |
| `Email__Smtp__Host`, `Port`, `User`, `Password`, `From` | **Leave `Host` empty for now.** Emails (confirm, reset password) are then written to the API log instead of sent; see step 5. Later: Brevo → SMTP & API. Use the SMTP host, port 587, your login, and an SMTP key from Brevo as the password. `From` is an address on your domain. |
| `Extraction__Provider` | `fake` (no AI calls, safe for testing) or `anthropic`. |
| `Extraction__ApiKey` | Your extraction API key. Leave it as the placeholder while `Extraction__Provider=fake`. |
| `Reports__IpHashSalt` | Run `openssl rand -hex 32` on the server, copy the output, and paste it here. Don't change it once the site is live. |

Save with **Ctrl+O**, **Enter**, then **Ctrl+X**.

## 4. First manual deploy

Do this once, by hand. After it works, GitHub does it for you (step 7).

The images are built by GitHub when the `main` branch passes CI. Before the first deploy, make sure a build has finished (repo → **Actions** → **Deploy** shows a green run). If the server says `denied` when it pulls, the images are still private. Do step 7c and then try again.

1. Log in as the `deploy` user, which is the account the automatic deploys use:
   ```bash
   ssh deploy@<ip>
   cd /opt/getabursary
   ```
2. Get the compose file and Caddy settings. These are the same files the automatic deploy copies:
   ```bash
   curl -fsSLO https://raw.githubusercontent.com/cssedi/getabursary/main/deploy/docker-compose.prod.yml
   curl -fsSLO https://raw.githubusercontent.com/cssedi/getabursary/main/deploy/Caddyfile
   ```
3. Set up the database tables and reference data. This runs once and exits:
   ```bash
   docker compose -f docker-compose.prod.yml --profile tools run --rm migrate
   ```
4. Start everything:
   ```bash
   docker compose -f docker-compose.prod.yml up -d
   ```
5. Check the logs. Press **Ctrl+C** to stop following them:
   ```bash
   docker compose -f docker-compose.prod.yml logs -f api
   ```
6. Check that it works. Open `https://<your domain>` in a browser. You should see the site. Then open `https://<your domain>/api/v1/health`. You should see a healthy status.

Use `-f docker-compose.prod.yml` on every command, because the file isn't named `docker-compose.yml` on the server.

## 5. Make yourself an admin

1. Put your own email address in `Admin__Emails` in `/opt/getabursary/.env`. Use a comma between addresses to add more than one.
2. Apply the change. A plain restart doesn't reread `.env`, so run migrate and then start again:
   ```bash
   cd /opt/getabursary
   docker compose -f docker-compose.prod.yml --profile tools run --rm migrate
   docker compose -f docker-compose.prod.yml up -d
   ```
3. On the site, click **Sign in → Create account** with that email address.
4. While mail isn't set up, the confirmation link goes to the log. On the server, run:
   ```bash
   docker compose -f docker-compose.prod.yml logs api | grep -i "confirm"
   ```
   Copy the `https://…/account/confirm?...` link from the output and open it in your browser. (Use the same trick for "forgot password" links.)
5. Sign in. You now have the admin role, and the account menu shows **Admin**.

## 6. When something goes wrong

- **`docker-ce` can't be found while the setup script runs (very new Ubuntu).** Docker may not publish packages for your Ubuntu version yet. Run `curl -fsSL https://get.docker.com | sh`, then run `bash bootstrap.sh` again.

- **The site is slow or out of memory.** Run `free -h` to see memory and swap, and `docker stats` to see what each container uses. Each container has a memory limit, so the one using the most is the one to look at. If the server is always full, it's too small for the load and needs a bigger Hetzner plan.
- **Cloudflare shows "502 Bad Gateway".** Cloudflare reached the server, but the site behind it didn't answer. Run `docker compose -f docker-compose.prod.yml ps` to see whether everything is `running`. Then check `logs -f caddy` and `logs -f api`. Also confirm that the SSL mode is **Full (strict)** and that `certs/origin.pem` and `origin.key` are the right files.
- **The site doesn't load at all.** Log in as root and check that the firewall still lets web traffic in with `ufw status`, and that the domain's DNS points at Cloudflare. It may take a few minutes after a change.
- **The first request after a quiet spell is slow.** That's expected. Neon's free database sleeps after 5 minutes without use and wakes in about half a second. If it stays slow, check that the connection string is the direct host with `SSL Mode=Require`.
- **You can't log in over SSH after running the script.** Your key was not in `/root/.ssh/authorized_keys` when you ran it, so password login is now off. Use your hosting provider's web console to log in and add the key.

## 7. Automatic deploys

Each push to `main` that passes CI builds three images (`getabursary-api`, `getabursary-worker` and `getabursary-web`) and sends them to GitHub's container registry. If the repo variable `DEPLOY` is `on`, the deploy workflow then copies the compose file and Caddyfile to the server, runs migrate, starts the new containers, and checks `/api/v1/health`. See `.github/workflows/deploy.yml` for the exact steps.

Set this up once, after step 4 works:

### 7a. Create a deploy key on your Mac

```bash
ssh-keygen -t ed25519 -f ~/.ssh/getabursary_deploy -N ""
```

This makes two files: `getabursary_deploy` (private, a secret) and `getabursary_deploy.pub` (public).

### 7b. Give the public key to the server

Log in as root, then append the public key to the `deploy` user's keys:

```bash
ssh root@<ip>
nano /home/deploy/.ssh/authorized_keys
```

On your Mac, run `cat ~/.ssh/getabursary_deploy.pub` and paste the whole line at the end of the file. Save and exit. Then check that the key works from your Mac:

```bash
ssh -i ~/.ssh/getabursary_deploy deploy@<ip> echo ok
```

It should print `ok` and not ask for a password.

### 7c. Add the secrets and variables on GitHub

On GitHub, open the repo → **Settings → Secrets and variables → Actions**.

On the **Secrets** tab, add:

- `DEPLOY_HOST`: the server's IPv4 address (`<ip>`).
- `DEPLOY_USER`: `deploy`.
- `DEPLOY_SSH_KEY`: the whole contents of `~/.ssh/getabursary_deploy` (the private key, including the `BEGIN` and `END` lines). Run `cat ~/.ssh/getabursary_deploy` to see it.
- `DEPLOY_KNOWN_HOSTS`: the output of `ssh-keyscan <ip>` (run it on your Mac and paste all the lines it prints).

On the **Variables** tab, add:

- `SITE_DOMAIN`: `example.co.za` (your domain without `www`).
- `DEPLOY`: `on`. Set this only after step 4 has worked by hand. Until it's `on`, the deploy workflow builds the images but doesn't touch the server.

After the first image build, the server must be able to pull the images without logging in. On GitHub, open your profile → **Packages**. For each of the 3 packages (`getabursary-api`, `getabursary-worker`, `getabursary-web`), click it, then **Package settings → Change visibility → Public**. The repo is public, so the images contain no secrets.

Secrets never go in the repo, issues or chat. If one is pasted somewhere public, make a new one and replace it.

### 7d. Roll back to an older version

If a new version breaks the site, go to GitHub → **Actions → Deploy → Run workflow**, and in the `image_tag` box enter the commit sha of the version you want (the long hash in the commit list). The workflow then deploys that version's images without building new ones.
