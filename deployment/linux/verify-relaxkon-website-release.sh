#!/usr/bin/env bash
# Verify that the GitHub Release asset is the exact expected website Server ZIP.
set -Eeuo pipefail

REPOSITORY='nanaminato/RelaxKonOS'
TAG='v0.1.1'
VERSION='0.1.1'
# SHA-256 of E:\riderprojects\RelaxKon\release\0.1.1\RelaxKonServer-0.1.1-linux-x64.zip
# before it was repackaged on 2026-09-15 at approximately 21:41 local time.
EXPECTED_SHA256='eb3c9af2489e78989ec799f3414f077a4bdf901317d4a99e5383461b671f73f0'

usage() {
  cat >&2 <<'EOF'
Usage: bash verify-relaxkon-website-release.sh [options]

Options:
  --repository OWNER/REPO  GitHub repository (default: nanaminato/RelaxKonOS)
  --tag TAG               Release tag (default: v0.1.1)
  --version VERSION       Asset version (default: 0.1.1)
  --expected-sha256 HASH  Expected 64-character SHA-256
EOF
  exit 64
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --repository) REPOSITORY=${2:-}; shift 2 ;;
    --tag) TAG=${2:-}; shift 2 ;;
    --version) VERSION=${2:-}; shift 2 ;;
    --expected-sha256) EXPECTED_SHA256=${2:-}; shift 2 ;;
    -h|--help) usage ;;
    *) usage ;;
  esac
done

[[ $REPOSITORY =~ ^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$ ]] || { echo 'Invalid --repository.' >&2; exit 64; }
[[ $TAG =~ ^[0-9A-Za-z][0-9A-Za-z._-]{0,63}$ && $VERSION =~ ^[0-9A-Za-z][0-9A-Za-z._-]{0,63}$ ]] || { echo 'Invalid --tag or --version.' >&2; exit 64; }
[[ $EXPECTED_SHA256 =~ ^[A-Fa-f0-9]{64}$ ]] || { echo 'Invalid --expected-sha256.' >&2; exit 64; }
for command in curl sha256sum unzip; do command -v "$command" >/dev/null || { echo "Missing required command: $command" >&2; exit 69; }; done

asset="RelaxKonServer-$VERSION-linux-x64.zip"
base="https://github.com/$REPOSITORY/releases/download/$TAG"
temporary_directory=$(mktemp -d)
cleanup() { rm -rf -- "$temporary_directory"; }
trap cleanup EXIT

echo "Downloading: $base/$asset"
curl --fail --location --retry 3 --retry-delay 2 --show-error --silent \
  "$base/$asset" --output "$temporary_directory/$asset"
curl --fail --location --retry 3 --retry-delay 2 --show-error --silent \
  "$base/$asset.sha256" --output "$temporary_directory/$asset.sha256"

actual_sha256=$(sha256sum "$temporary_directory/$asset" | awk '{print tolower($1)}')
expected_sha256=${EXPECTED_SHA256,,}
published_sha256=$(awk -v name="$asset" '$2 == name { print tolower($1); exit }' "$temporary_directory/$asset.sha256")
echo "Expected SHA-256: $expected_sha256"
echo "Actual SHA-256:   $actual_sha256"
echo "Published SHA-256: ${published_sha256:-missing}"
[[ $actual_sha256 == "$expected_sha256" ]] || {
  echo 'FAILED: the downloaded GitHub asset is not the expected 21:41 package.' >&2
  exit 65
}
[[ $published_sha256 == "$actual_sha256" ]] || {
  echo 'FAILED: the GitHub .sha256 file does not match its ZIP asset.' >&2
  exit 65
}

unzip -tq "$temporary_directory/$asset" >/dev/null
entries=$(unzip -Z1 "$temporary_directory/$asset")
grep -Fxq 'RelaxKonServer' <<<"$entries" || { echo 'FAILED: ZIP root does not contain RelaxKonServer.' >&2; exit 65; }
grep -Fxq 'appsettings.json' <<<"$entries" || { echo 'FAILED: ZIP root does not contain appsettings.json.' >&2; exit 65; }

echo 'PASSED: GitHub serves the expected package and its required root files.'
