---
title: Remote installation: step by step
description: Remote installation: step by step
category: Getting Started
order: 9
---

# Remote installation: step by step

Start with a remote host that has no RelaxKonOS Server, and install through the desktop client's Server Center. Android layouts differ, but sources, modes, receipts and health checks follow the same principles. For local Windows 10/11, use [Manage this PC](/docs/en-US/latest/getting-started/windows); SSH is unnecessary.

## 1. Prepare the host and credentials

1. Get a client matching your device from [Downloads](/downloads). Record the target address, SSH port, management account and usable password or private key.
2. Enable SSH on the target and confirm the client can reach its port. An existing Server API is unnecessary for first installation.
3. Obtain the SSH host-key fingerprint from a host console or trusted administrator. If it changes, confirm whether the host was reinstalled, its key rotated or its address changed.
4. Check target architecture, free space and account quota, and the intended Server port. Remote installation defaults to 5000; choose another if occupied.

| Target mode | Prerequisites | Choose when |
| --- | --- | --- |
| Linux System | Supported Linux, systemd, Python 3, root or usable sudo | System services, multiple users and explicitly authorized host management are needed |
| Linux User | Non-root account; Bash, Python 3, curl, unzip, realpath, stat, find, sha256sum, flock | Installing for the current user without sudo or systemd |
| Windows System | System PowerShell and an elevated administrator SSH session | Remotely managing system services on Windows 10/11 or Windows Server |

Linux System Mode defaults to Debian 12 and Ubuntu 22.04/24.04/26.04. Other systems require explicit advanced-option approval and still pass dependency and privilege checks. Windows Personal Mode uses the target PC's local entry, not this SSH flow.

## 2. Add and trust the SSH host

1. Open Server Center / Install or manage a server; the desktop sign-in page also offers SSH.
2. Enter address, SSH port and account; choose password or private-key authentication supported by the client. Enter a key passphrase if required.
3. Compare the displayed fingerprint, then trust and connect only after identifying the host. Do not bypass verification for a changed key.
4. When saving a connection, check whether credentials are being saved. Secrets use local secure storage and are not workspace-synced.

![Screenshot placeholder：Capture: SSH host form and first-trust dialog. Show address, port, account and fingerprint; redact real addresses and credentials.](/assets/docs/screenshots/en-US/remote-host.svg)

> Capture: SSH host form and first-trust dialog. Show address, port, account and fingerprint; redact real addresses and credentials.

## 3. Run preflight and select the mode

Select the host and check its environment. Read OS, architecture, privileges, dependencies, installation state and port results; resolve failed checks first. Linux System Mode can use a normal SSH account with sudo; a blank sudo password tries the current SSH password. Windows System Mode requires an elevated SSH session. Linux User Mode rejects root.

If an installation exists, verify its identity, mode and directories and follow [maintenance](/docs/en-US/latest/apps/server-maintenance) instead of overwriting another installation as a first install.

![Screenshot placeholder：Capture: preflight results and mode selection, including OS, architecture, privileges, dependencies and existing installation state.](/assets/docs/screenshots/en-US/remote-preflight.svg)

> Capture: preflight results and mode selection, including OS, architecture, privileges, dependencies and existing installation state.

## 4. Choose a package source

| Source | Your input | Where transfer happens |
| --- | --- | --- |
| Official stable | Official source, optional HTTPS catalog | Target downloads and verifies the official ZIP digest and file inventory |
| Local bundle | ZIP selected on your device | Client uploads to the target; useful when the target cannot reach the Internet |
| Bundle on server | Absolute ZIP path on the target | Target reads its own file without a client upload |
| Custom HTTPS | ZIP HTTPS URL and full SHA-256 | Target downloads and verifies the supplied digest |

Use `*-server.zip` for Windows/Linux System Mode and `*-user-server.zip` for Linux User Mode. A `*-client.zip` cannot install Server. Every source retains package-kind and architecture checks. Local/server ZIPs still require necessary files, safe extraction and valid manifest structure. Available versions follow the catalog; source documentation does not prove a feature exists in an older artifact.

![Screenshot placeholder：Capture: the four sources and selected package. Show kind, version and architecture; hide URLs containing credentials.](/assets/docs/screenshots/en-US/remote-source.svg)

> Capture: the four sources and selected package. Show kind, version and architecture; hide URLs containing credentials.

## 5. Configure directories, network, TLS and permissions

1. Set program/data directories and Server port using absolute paths on the target. Linux User Mode also offers XDG configuration/state/cache roots; avoid overlaps.
2. Loopback is suitable for access on that host. For other devices choose LAN and HTTPS with a certificate covering the actual IP or hostname. LAN listening does not imply firewall access.
3. System Mode offers no certificate, custom PFX/P12 or PEM chain/private key, or self-signed TLS. Certificate passwords are excluded from the review summary.
4. Review file scopes and grant only needed paths. Linux administrator/root scopes are configured separately. Docker authorization is off by default and requires opt-in.
5. Linux User Mode defaults to loopback and can use a client-managed SSH tunnel. See [User Mode](/docs/en-US/latest/getting-started/user-mode) for boundaries.

![Screenshot placeholder：Capture: installation options with port, directories, listening scope, TLS and file permissions; redact certificate passwords.](/assets/docs/screenshots/en-US/remote-options.svg)

> Capture: installation options with port, directories, listening scope, TLS and file permissions; redact certificate passwords.

## 6. Review, execute and verify

Recheck host, mode, source, roots and port before submission. Perform one operation on this installation at a time; do not submit duplicates from another client. Observe transfer, execution and verification in order. Downloads with unknown totals may show no percentage.

![Screenshot placeholder：Capture: final review with host, mode, source, directories and port summary.](/assets/docs/screenshots/en-US/remote-review.svg)

> Capture: final review with host, mode, source, directories and port summary.

100% upload/download only completes transfer. Success requires a successful host receipt, installed state and an independent health check. Keep the operation ID. On failure, inspect its stage, problem code and logs; a closed window or launcher exit code is insufficient.

![Screenshot placeholder：Capture: result and health verification with operation ID, stage, receipt, installed state and health check.](/assets/docs/screenshots/en-US/remote-result.svg)

> Capture: result and health verification with operation ID, stage, receipt, installed state and health check.

## 7. Connect to the installed Server

Return to Server sign-in, use the configured reachable address and a supported system account or Alias. Loopback-only installations and Linux User Mode can use an SSH tunnel. Enter the Server's loopback address on the SSH host and selected port, such as `http://127.0.0.1:5000`. HTTPS names and trust chains still require verification. The SSH desktop itself is not a Server workspace.

## Troubleshooting order

| Symptom | Next step |
| --- | --- |
| SSH failure or changed fingerprint | Check address, SSH port, account and host key; establish identity before retrying |
| sudo/administrator check fails | Verify the session satisfies the chosen mode; do not elevate everyday Server operation to bypass it |
| Wrong kind, architecture or digest | Recheck ZIP source, RID, mode and digest; keep official verification enabled |
| Insufficient space or quota | Check target staging volume and account quota, free space and run preflight again |
| Interrupted or unknown result | Reconnect to the same host, refresh state and the original receipt before submitting another operation |
| Installed but unreachable | Check listening address, port, firewall, TLS and client address before reinstalling |

Next: [Update, uninstall and maintenance](/docs/en-US/latest/apps/server-maintenance) · [Sign-in](/docs/en-US/latest/getting-started/login).
