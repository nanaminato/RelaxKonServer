#!/usr/bin/env bash
# Explicit update workflow: uninstall the running service, then install a new GitHub release.
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

"$SCRIPT_DIRECTORY/uninstall-relaxkon-website.sh" --root "$root" --for-update
"$SCRIPT_DIRECTORY/install-relaxkon-website.sh" "${arguments[@]}" --skip-packages

# The installer writes the HTTP bootstrap Nginx configuration. Restore HTTPS automatically
# only when the conventional Let's Encrypt certificate is still present.
if [[ -r "/etc/letsencrypt/live/$domain/fullchain.pem" && -r "/etc/letsencrypt/live/$domain/privkey.pem" ]]; then
  "$SCRIPT_DIRECTORY/enable-relaxkon-https.sh" --root "$root" --domain "$domain" --www-domain "$www_domain" --downloads-domain "$downloads_domain" --api-port "$api_port"
else
  echo 'Update installed, but HTTPS was not re-enabled because the expected certificate was not found.' >&2
fi
