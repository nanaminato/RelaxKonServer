#!/usr/bin/env bash
# Destructive update workflow: remove the running deployment, then install a new GitHub release.
set -Eeuo pipefail

SCRIPT_DIRECTORY=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
[[ ${EUID:-$(id -u)} -eq 0 ]] || { echo 'Run with sudo.' >&2; exit 1; }
[[ $# -gt 0 ]] || { echo 'Usage: sudo bash update-relaxkon-website.sh --version VERSION (--repository OWNER/REPO --tag TAG | --release-base-uri URL) [install options]' >&2; exit 64; }

root=/srv/relaxkon
domain=relaxkon.com
www_domain=www.relaxkon.com
downloads_domain=downloads.relaxkon.com
api_port=5062
arguments=("$@")
index=0
while (( index < ${#arguments[@]} )); do
  case "${arguments[$index]}" in
    --root) ((index + 1 < ${#arguments[@]})) || { echo '--root requires a value.' >&2; exit 64; }; root=${arguments[$((index + 1))]}; ((index += 2)) ;;
    --domain) ((index + 1 < ${#arguments[@]})) || { echo '--domain requires a value.' >&2; exit 64; }; domain=${arguments[$((index + 1))]}; ((index += 2)) ;;
    --www-domain) ((index + 1 < ${#arguments[@]})) || { echo '--www-domain requires a value.' >&2; exit 64; }; www_domain=${arguments[$((index + 1))]}; ((index += 2)) ;;
    --downloads-domain) ((index + 1 < ${#arguments[@]})) || { echo '--downloads-domain requires a value.' >&2; exit 64; }; downloads_domain=${arguments[$((index + 1))]}; ((index += 2)) ;;
    --api-port) ((index + 1 < ${#arguments[@]})) || { echo '--api-port requires a value.' >&2; exit 64; }; api_port=${arguments[$((index + 1))]}; ((index += 2)) ;;
    *) ((index += 1)) ;;
  esac
done

# A website release contains the API Content tree, including ReleaseDelivery.
# Do not retain an earlier Content tree when replacing a release: its artifacts,
# descriptors and bootstrap scripts must come exclusively from the new package.
bash "$SCRIPT_DIRECTORY/uninstall-relaxkon-website.sh" --root "$root" --remove-releases
bash "$SCRIPT_DIRECTORY/install-relaxkon-website.sh" "${arguments[@]}" --skip-packages

# The installer leaves an HTTP-only Nginx configuration in place, which is required
# for ACME HTTP-01. Reuse an existing certificate or issue a new one before enabling
# HTTPS. Certbot remains interactive when it needs the operator's registration details.
certificate="/etc/letsencrypt/live/$domain/fullchain.pem"
certificate_key="/etc/letsencrypt/live/$domain/privkey.pem"
if [[ ! -r $certificate || ! -r $certificate_key ]]; then
  if ! command -v certbot >/dev/null 2>&1; then
    apt-get update
    DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends certbot
  fi
  echo "Issuing a Let's Encrypt certificate for $domain, $www_domain, and $downloads_domain."
  certbot certonly --webroot --webroot-path "$root/acme" \
    --domain "$domain" \
    --domain "$www_domain" \
    --domain "$downloads_domain"
fi
bash "$SCRIPT_DIRECTORY/enable-relaxkon-https.sh" --root "$root" --domain "$domain" --www-domain "$www_domain" --downloads-domain "$downloads_domain" --api-port "$api_port"
