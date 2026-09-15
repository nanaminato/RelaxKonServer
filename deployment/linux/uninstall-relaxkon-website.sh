#!/usr/bin/env bash
# Remove the website deployment. --for-update keeps Nginx, releases and ACME files.
set -Eeuo pipefail

ROOT=/srv/relaxkon
FOR_UPDATE=false
REMOVE_RELEASES=false
usage() { echo 'Usage: sudo bash uninstall-relaxkon-website.sh [--root PATH] [--for-update | --remove-releases]' >&2; exit 64; }
while [[ $# -gt 0 ]]; do
  case "$1" in
    --root) ROOT=${2:-}; shift 2 ;; --for-update) FOR_UPDATE=true; shift ;; --remove-releases) REMOVE_RELEASES=true; shift ;; -h|--help) usage ;; *) usage ;;
  esac
done
[[ ${EUID:-$(id -u)} -eq 0 ]] || { echo 'Run with sudo.' >&2; exit 1; }
[[ $ROOT == /* && $ROOT != / ]] || { echo '--root must be absolute and not /.' >&2; exit 1; }
[[ $FOR_UPDATE == false || $REMOVE_RELEASES == false ]] || { echo '--for-update and --remove-releases cannot be combined.' >&2; exit 64; }

systemctl disable --now relaxkon-server.service 2>/dev/null || true
rm -f /etc/systemd/system/relaxkon-server.service
systemctl daemon-reload
if [[ $FOR_UPDATE == true ]]; then
  rm -f "$ROOT/api/current" "$ROOT/frontend/current"
  echo 'Old service stopped. Nginx, release archives and ACME files were retained for the update.'
  exit 0
fi
rm -f /etc/nginx/sites-enabled/relaxkon.conf /etc/nginx/sites-available/relaxkon.conf
if [[ -x $(command -v nginx) ]]; then nginx -t && systemctl reload nginx || true; fi
if [[ $REMOVE_RELEASES == true ]]; then
  [[ -d $ROOT/api/releases && -d $ROOT/frontend/releases ]] || { echo 'Refusing to remove an unrecognised deployment root.' >&2; exit 1; }
  rm -rf -- "$ROOT/api/releases" "$ROOT/frontend/releases"
  rm -f "$ROOT/api/current" "$ROOT/frontend/current"
  echo 'RelaxKon service, Nginx site configuration, and all API/frontend release archives have been removed.'
  echo 'ACME files were not deleted.'
  exit 0
fi
echo 'RelaxKon service and Nginx site configuration have been removed.'
echo 'ACME files and retained releases were not deleted. Use --remove-releases only after backing up any required release-delivery data.'
