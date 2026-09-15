#!/usr/bin/env bash
# Install the RelaxKon website API and Angular site from a GitHub Release.
# Run as root on Debian 12 or Ubuntu 22.04/24.04/26.04.
set -Eeuo pipefail

REPOSITORY=""
TAG=""
VERSION=""
DOMAIN="relaxkon.com"
WWW_DOMAIN="www.relaxkon.com"
DOWNLOADS_DOMAIN="downloads.relaxkon.com"
API_PORT=5062
ROOT=/srv/relaxkon
SKIP_PACKAGES=false

usage() {
  cat >&2 <<'EOF'
Usage: sudo bash install-relaxkon-website.sh --repository OWNER/REPO --tag TAG --version VERSION [options]

Required:
  --repository OWNER/REPO  GitHub repository containing the release assets
  --tag TAG               GitHub Release tag, for example v0.1.0
  --version VERSION       Artifact version, for example 0.1.0

Options:
  --domain NAME           Main domain (default: relaxkon.com)
  --www-domain NAME       www domain (default: www.relaxkon.com)
  --downloads-domain NAME Download domain (default: downloads.relaxkon.com)
  --api-port PORT         Local API port (default: 5062)
  --root PATH             Deployment root (default: /srv/relaxkon)
  --skip-packages         Do not run apt-get; used by the update script

The GitHub Release must contain these four assets:
  RelaxKonServer-VERSION-linux-x64.zip and its .sha256 file
  RelaxKon-web-VERSION.zip and its .sha256 file

For a private GitHub release, export GITHUB_TOKEN before running this script.
EOF
  exit 64
}

die() { echo "ERROR: $*" >&2; exit 1; }
require_root() { [[ ${EUID:-$(id -u)} -eq 0 ]] || die 'Run this script with sudo.'; }
valid_domain() { [[ $1 =~ ^[A-Za-z0-9]([A-Za-z0-9-]{0,61}[A-Za-z0-9])?(\.[A-Za-z0-9]([A-Za-z0-9-]{0,61}[A-Za-z0-9])?)+$ ]]; }
valid_component() { [[ $1 =~ ^[0-9A-Za-z][0-9A-Za-z._-]{0,63}$ ]]; }

while [[ $# -gt 0 ]]; do
  case "$1" in
    --repository) REPOSITORY=${2:-}; shift 2 ;;
    --tag) TAG=${2:-}; shift 2 ;;
    --version) VERSION=${2:-}; shift 2 ;;
    --domain) DOMAIN=${2:-}; shift 2 ;;
    --www-domain) WWW_DOMAIN=${2:-}; shift 2 ;;
    --downloads-domain) DOWNLOADS_DOMAIN=${2:-}; shift 2 ;;
    --api-port) API_PORT=${2:-}; shift 2 ;;
    --root) ROOT=${2:-}; shift 2 ;;
    --skip-packages) SKIP_PACKAGES=true; shift ;;
    -h|--help) usage ;;
    *) usage ;;
  esac
done

require_root
[[ $REPOSITORY =~ ^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$ ]] || die '--repository must be OWNER/REPO.'
valid_component "$TAG" && valid_component "$VERSION" || die 'Tag and version contain unsupported characters.'
valid_domain "$DOMAIN" && valid_domain "$WWW_DOMAIN" && valid_domain "$DOWNLOADS_DOMAIN" || die 'Invalid domain name.'
[[ $API_PORT =~ ^[0-9]+$ ]] && (( API_PORT >= 1 && API_PORT <= 65535 )) || die 'Invalid API port.'
[[ $ROOT == /* && $ROOT != / ]] || die '--root must be an absolute, non-root directory.'

if [[ $SKIP_PACKAGES == false ]]; then
  export DEBIAN_FRONTEND=noninteractive
  apt-get update
  apt-get install -y --no-install-recommends ca-certificates curl nginx unzip
fi
for command in curl unzip sha256sum systemctl nginx install; do command -v "$command" >/dev/null || die "Required command is missing: $command"; done

SERVER_ASSET="RelaxKonServer-$VERSION-linux-x64.zip"
WEB_ASSET="RelaxKon-web-$VERSION.zip"
RELEASE_BASE="https://github.com/$REPOSITORY/releases/download/$TAG"
TEMPORARY_DIRECTORY=$(mktemp -d)
cleanup() { rm -rf -- "$TEMPORARY_DIRECTORY"; }
trap cleanup EXIT

download_asset() {
  local asset=$1 destination=$2
  local -a curl_args=(--fail --location --retry 3 --retry-delay 2 --show-error --silent)
  if [[ -n ${GITHUB_TOKEN:-} ]]; then curl_args+=(--header "Authorization: Bearer $GITHUB_TOKEN"); fi
  curl "${curl_args[@]}" "$RELEASE_BASE/$asset" --output "$destination"
}

verify_and_unpack() {
  local asset=$1 destination=$2 expected actual
  download_asset "$asset" "$TEMPORARY_DIRECTORY/$asset"
  download_asset "$asset.sha256" "$TEMPORARY_DIRECTORY/$asset.sha256"
  expected=$(awk -v name="$asset" '$2 == name { print $1; exit }' "$TEMPORARY_DIRECTORY/$asset.sha256")
  [[ $expected =~ ^[A-Fa-f0-9]{64}$ ]] || die "Invalid checksum file for $asset."
  actual=$(sha256sum "$TEMPORARY_DIRECTORY/$asset" | awk '{print $1}')
  [[ ${actual,,} == ${expected,,} ]] || die "Checksum verification failed for $asset."
  unzip -tq "$TEMPORARY_DIRECTORY/$asset" >/dev/null
  install -d -m 0755 "$destination"
  unzip -q "$TEMPORARY_DIRECTORY/$asset" -d "$destination"
}

install -d -o root -g root -m 0755 "$ROOT/api/releases" "$ROOT/frontend/releases" "$ROOT/acme"
API_RELEASE="$ROOT/api/releases/$VERSION"
WEB_RELEASE="$ROOT/frontend/releases/$VERSION"
[[ ! -e $API_RELEASE && ! -e $WEB_RELEASE ]] || die "Version $VERSION is already installed. Use a new version or remove the incomplete release directory after inspection."

verify_and_unpack "$SERVER_ASSET" "$API_RELEASE"
verify_and_unpack "$WEB_ASSET" "$WEB_RELEASE"
[[ -x $API_RELEASE/RelaxKonServer ]] || die 'Server archive does not contain an executable RelaxKonServer at its root.'
[[ -f $API_RELEASE/appsettings.json && -d $API_RELEASE/Content && -f $WEB_RELEASE/index.html ]] || die 'Release archive layout is incomplete.'
chown -R root:root "$API_RELEASE" "$WEB_RELEASE"
chmod -R go-w "$API_RELEASE" "$WEB_RELEASE"

if ! id -u relaxkon >/dev/null 2>&1; then
  useradd --system --home-dir /nonexistent --shell /usr/sbin/nologin relaxkon
fi
chown -R relaxkon:relaxkon "$API_RELEASE"
chmod -R go-w "$API_RELEASE"

switch_link() {
  local target=$1 link=$2 next="$2.next"
  ln -s "$target" "$next"
  mv -Tf "$next" "$link"
}
switch_link "$API_RELEASE" "$ROOT/api/current"
switch_link "$WEB_RELEASE" "$ROOT/frontend/current"

cat >/etc/systemd/system/relaxkon-server.service <<EOF
[Unit]
Description=RelaxKon Website API
After=network-online.target
Wants=network-online.target

[Service]
Type=simple
User=relaxkon
Group=relaxkon
WorkingDirectory=$ROOT/api/current
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://127.0.0.1:$API_PORT
ExecStart=$ROOT/api/current/RelaxKonServer
Restart=on-failure
RestartSec=5
NoNewPrivileges=true
PrivateTmp=true
ProtectSystem=strict
ProtectHome=true
ReadWritePaths=$ROOT/api/current

[Install]
WantedBy=multi-user.target
EOF

cat >/etc/nginx/sites-available/relaxkon.conf <<EOF
# HTTP-only bootstrap configuration. Keep this until a certificate has been issued.
server {
    listen 80;
    listen [::]:80;
    server_name $DOMAIN $WWW_DOMAIN $DOWNLOADS_DOMAIN;
    root $ROOT/frontend/current;
    index index.html;

    location ^~ /.well-known/acme-challenge/ { root $ROOT/acme; }
    location /api/ { include proxy_params; proxy_pass http://127.0.0.1:$API_PORT; proxy_set_header X-Forwarded-Proto \$scheme; }
    location /relaxkonos/ { include proxy_params; proxy_pass http://127.0.0.1:$API_PORT; proxy_set_header X-Forwarded-Proto \$scheme; proxy_request_buffering off; proxy_buffering off; }
    location /apt/ { include proxy_params; proxy_pass http://127.0.0.1:$API_PORT; proxy_set_header X-Forwarded-Proto \$scheme; proxy_request_buffering off; proxy_buffering off; }
    location / { try_files \$uri \$uri/ /index.html; }
}
EOF
ln -sfn /etc/nginx/sites-available/relaxkon.conf /etc/nginx/sites-enabled/relaxkon.conf
rm -f /etc/nginx/sites-enabled/default
nginx -t
systemctl daemon-reload
systemctl enable --now relaxkon-server.service nginx.service
sleep 2
curl --fail --silent --show-error "http://127.0.0.1:$API_PORT/api/health" >/dev/null || die 'The local API health check failed. Run: journalctl -u relaxkon-server -n 100 --no-pager'
systemctl reload nginx

cat <<EOF
Installed RelaxKon $VERSION.
HTTP is ready. Before enabling public download/install URLs:
  1. Point A/AAAA records for $DOMAIN, $WWW_DOMAIN and $DOWNLOADS_DOMAIN to this server.
  2. Issue one certificate that covers all three names, for example:
     sudo apt-get install -y certbot
     sudo certbot certonly --webroot -w $ROOT/acme -d $DOMAIN -d $WWW_DOMAIN -d $DOWNLOADS_DOMAIN
  3. Run enable-relaxkon-https.sh with the same domains.
EOF
