#!/usr/bin/env bash
# Bootstraps a fresh Ubuntu server for GetABursary (spec 08 §2c, docs/DEPLOY.md step 2).
# Run by .github/workflows/deploy.yml as root whenever this file changes (or by hand: bash bootstrap.sh).
# Safe to run again: every step checks whether it is already done, so a second run changes nothing.
set -euo pipefail

APP_DIR=/opt/getabursary
export DEBIAN_FRONTEND=noninteractive

if [[ $EUID -ne 0 ]]; then
  echo "Run this as root." >&2
  exit 1
fi

# shellcheck source=/dev/null
. /etc/os-release
if [[ ${ID:-} != ubuntu ]]; then
  echo "This script is for Ubuntu (found: ${ID:-unknown})." >&2
  exit 1
fi

# Check this before changing anything: disabling password login is only safe if a key can log in.
if [[ ! -s /root/.ssh/authorized_keys ]]; then
  echo "/root/.ssh/authorized_keys is empty. Add your SSH key first, or you will be locked out." >&2
  exit 1
fi

echo "==> System packages"
apt-get update
apt-get -y upgrade
apt-get install -y ca-certificates curl ufw unattended-upgrades

echo "==> Docker Engine and compose plugin (Docker's apt repo)"
install -d -m 0755 /etc/apt/keyrings
if [[ ! -f /etc/apt/keyrings/docker.asc ]]; then
  curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc
  chmod a+r /etc/apt/keyrings/docker.asc
fi
codename=${UBUNTU_CODENAME:-$VERSION_CODENAME}
# Very new Ubuntu releases can be missing from Docker's repo for a while; the 24.04 (noble) packages work on them.
curl -fsI "https://download.docker.com/linux/ubuntu/dists/$codename/Release" >/dev/null || codename=noble
printf 'deb [arch=%s signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/ubuntu %s stable\n' \
  "$(dpkg --print-architecture)" "$codename" > /etc/apt/sources.list.d/docker.list
apt-get update
apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
systemctl enable --now docker

echo "==> Automatic security updates"
cat > /etc/apt/apt.conf.d/20auto-upgrades <<'EOF'
APT::Periodic::Update-Package-Lists "1";
APT::Periodic::Unattended-Upgrade "1";
EOF

echo "==> 2 GB swap file"
if [[ ! -f /swapfile ]]; then
  fallocate -l 2G /swapfile
  chmod 600 /swapfile
  mkswap /swapfile
fi
grep -q '^/swapfile ' /etc/fstab || echo '/swapfile none swap sw 0 0' >> /etc/fstab
grep -q '^/swapfile ' /proc/swaps || swapon /swapfile
printf 'vm.swappiness=10\n' > /etc/sysctl.d/99-getabursary.conf
sysctl -q -p /etc/sysctl.d/99-getabursary.conf

echo "==> deploy user (docker group, same keys as root)"
if ! id deploy >/dev/null 2>&1; then
  useradd --create-home --shell /bin/bash deploy
fi
usermod -aG docker deploy
install -d -m 700 -o deploy -g deploy /home/deploy/.ssh
touch /home/deploy/.ssh/authorized_keys
chown deploy:deploy /home/deploy/.ssh/authorized_keys
chmod 600 /home/deploy/.ssh/authorized_keys
# Append only the keys that are missing, so a key added later for GitHub Actions survives re-runs.
while IFS= read -r key; do
  [[ -n $key ]] || continue
  grep -qxF -- "$key" /home/deploy/.ssh/authorized_keys || echo "$key" >> /home/deploy/.ssh/authorized_keys
done < /root/.ssh/authorized_keys

echo "==> SSH: keys only, no passwords"
# 00- so it is read before Ubuntu's and cloud-init's drop-ins (the first value sshd reads wins).
cat > /etc/ssh/sshd_config.d/00-getabursary.conf <<'EOF'
PasswordAuthentication no
KbdInteractiveAuthentication no
EOF
sshd -t
systemctl reload ssh
sshd -T | grep -qx 'passwordauthentication no' || {
  echo "sshd still allows passwords. Check /etc/ssh/sshd_config.d/." >&2
  exit 1
}

echo "==> Firewall: SSH from anywhere, web only from Cloudflare"
cf_v4=$(curl -fsSL https://www.cloudflare.com/ips-v4)
cf_v6=$(curl -fsSL https://www.cloudflare.com/ips-v6)
cf_ranges=$(printf '%s\n%s\n' "$cf_v4" "$cf_v6" | grep -E '^[0-9a-fA-F:.]+/[0-9]+$')
ufw default deny incoming
ufw default allow outgoing
ufw allow 22/tcp
while read -r cidr; do
  for port in 80 443; do
    ufw allow proto tcp from "$cidr" to any port "$port"
  done
done <<< "$cf_ranges"
# Docker publishes ports through its own iptables rules, which skip ufw's INPUT rules. Limit them
# in DOCKER-USER too (the ufw-docker pattern). Only IPv4 here: Docker doesn't publish IPv6 by default.
# Rewritten on every run so the Cloudflare ranges stay current.
public_if=$(ip -4 route show default | awk 'NR==1 {print $5}')
[[ -n $public_if ]] || { echo "No default network interface found." >&2; exit 1; }
sed -i '/^# BEGIN GETABURSARY/,/^# END GETABURSARY/d' /etc/ufw/after.rules
{
  echo '# BEGIN GETABURSARY (deploy/bootstrap.sh): Docker bypasses ufw, so limit published web ports here'
  echo '*filter'
  echo ':DOCKER-USER - [0:0]'
  while read -r cidr; do
    echo "-A DOCKER-USER -i $public_if -p tcp -m multiport --dports 80,443 -s $cidr -j RETURN"
  done < <(grep -v ':' <<< "$cf_ranges")
  echo "-A DOCKER-USER -i $public_if -p tcp -m multiport --dports 80,443 -j DROP"
  echo 'COMMIT'
  echo '# END GETABURSARY'
} >> /etc/ufw/after.rules
ufw --force enable
ufw reload

echo "==> App folder"
install -d -m 755 -o deploy -g deploy "$APP_DIR"

echo "==> Done."
