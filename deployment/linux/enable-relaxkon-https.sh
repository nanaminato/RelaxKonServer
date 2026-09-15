#!/usr/bin/env bash
# Enable HTTPS only after a certificate covering every configured host exists.
set -Eeuo pipefail

DOMAIN=relaxkon.com
WWW_DOMAIN=www.relaxkon.com
DOWNLOADS_DOMAIN=downloads.relaxkon.com
API_PORT=5062
ROOT=/srv/relaxkon
CERTIFICATE_FILE=""
CERTIFICATE_KEY=""

usage() { echo 'Usage: sudo bash enable-relaxkon-https.sh [--domain NAME --www-domain NAME --downloads-domain NAME --certificate FILE --certificate-key FILE --api-port PORT --root PATH]' >&2; exit 64; }
while [[ $# -gt 0 ]]; do
  case "$1" in
    --domain) DOMAIN=${2:-}; shift 2 ;; --www-domain) WWW_DOMAIN=${2:-}; shift 2 ;; --downloads-domain) DOWNLOADS_DOMAIN=${2:-}; shift 2 ;;
    --certificate) CERTIFICATE_FILE=${2:-}; shift 2 ;; --certificate-key) CERTIFICATE_KEY=${2:-}; shift 2 ;;
    --api-port) API_PORT=${2:-}; shift 2 ;; --root) ROOT=${2:-}; shift 2 ;; -h|--help) usage ;; *) usage ;;
  esac
done
[[ ${EUID:-$(id -u)} -eq 0 ]] || { echo 'Run with sudo.' >&2; exit 1; }
[[ -n $CERTIFICATE_FILE ]] || CERTIFICATE_FILE="/etc/letsencrypt/live/$DOMAIN/fullchain.pem"
[[ -n $CERTIFICATE_KEY ]] || CERTIFICATE_KEY="/etc/letsencrypt/live/$DOMAIN/privkey.pem"
[[ -r $CERTIFICATE_FILE && -r $CERTIFICATE_KEY ]] || { echo 'Certificate file or private key is not readable.' >&2; exit 1; }
[[ -d $ROOT/frontend/current && -f /etc/nginx/sites-available/relaxkon.conf ]] || { echo 'RelaxKon HTTP deployment was not found.' >&2; exit 1; }

cat >/etc/nginx/sites-available/relaxkon.conf <<EOF
server {
    listen 80;
    listen [::]:80;
    server_name $DOMAIN $WWW_DOMAIN $DOWNLOADS_DOMAIN;
    location ^~ /.well-known/acme-challenge/ { root $ROOT/acme; }
    location / { return 301 https://\$host\$request_uri; }
}
server {
    listen 443 ssl http2;
    listen [::]:443 ssl http2;
    server_name $DOMAIN $WWW_DOMAIN $DOWNLOADS_DOMAIN;
    ssl_certificate $CERTIFICATE_FILE;
    ssl_certificate_key $CERTIFICATE_KEY;
    root $ROOT/frontend/current;
    index index.html;

    location /api/ { include proxy_params; proxy_pass http://127.0.0.1:$API_PORT; proxy_set_header X-Forwarded-Proto \$scheme; }
    location /relaxkonos/ { include proxy_params; proxy_pass http://127.0.0.1:$API_PORT; proxy_set_header X-Forwarded-Proto \$scheme; proxy_request_buffering off; proxy_buffering off; }
    location /apt/ { include proxy_params; proxy_pass http://127.0.0.1:$API_PORT; proxy_set_header X-Forwarded-Proto \$scheme; proxy_request_buffering off; proxy_buffering off; }
    location / { try_files \$uri \$uri/ /index.html; }
}
EOF
nginx -t
systemctl reload nginx
curl --fail --silent --show-error "https://$DOMAIN/api/health" >/dev/null
cat <<EOF
HTTPS is active. Online installer endpoints are now available, for example:
  curl -fsSL https://$DOWNLOADS_DOMAIN/relaxkonos/stable/latest/bootstrap/install-relaxkonos.sh | sudo bash
  irm https://$DOWNLOADS_DOMAIN/relaxkonos/stable/latest/install.ps1 | iex
EOF
