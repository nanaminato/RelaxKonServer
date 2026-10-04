---
title: Installation
description: Prepare and install the RelaxKonOS client and server.
category: Getting Started
order: 2
---

# Installation

RelaxKonOS consists of a client and a server. Install the client on your device, then use **Server Center** to install or maintain the server.

## Local Windows 10/11 installation

On your own Windows 10/11 PC, choose **Manage this PC** on the desktop sign-in page. Personal Mode is the default, with System Mode also available; SSH is unnecessary. The current user owns Personal Mode, while installation and maintenance use UAC to configure a privileged helper. Server starts at sign-in and stops at sign-out. Authorize this PC through loopback after installation, then pair other devices with a code. See [Windows 10/11 personal computers](/docs/en-US/latest/getting-started/windows) for steps, mode differences and permission scopes. Windows Server supports System Mode only.

## Before installation

- Get the matching client from [Downloads](/downloads). Published packages require no .NET SDK. See [Android](/docs/en-US/latest/getting-started/android) for mobile installation.
- Remote installation requires SSH and a management account on the target host; verify the host key fingerprint. Local Windows 10/11 management requires no SSH.
- Linux System Mode defaults to Debian 12 and Ubuntu 22.04/24.04/26.04; it requires root or approved sudo, systemd and Python 3. Other Linux systems require an explicit advanced-option choice.
- Linux User Mode requires a non-root account, Bash, Python 3, curl, unzip, realpath, stat, find, sha256sum and flock. It needs no sudo or systemd.
- Windows 10/11 supports Personal and System Modes; Windows Server supports System Mode only. Remote Windows System Mode installation requires an elevated administrator SSH session; local installation uses administrator privileges. Both require system PowerShell.
- Check architecture, space, dependencies and ports. Remote first installation uses SSH; local Windows management invokes the local deployment engine.

## Remote installation from the client (SSH)

1. Open **Server Center / Install or manage a server**. Desktop users can also select SSH in the login window and open Server Center after connecting.
2. Add the host with its SSH address, port and account; verify and trust its host key.
3. Run preflight and choose **Install server**. An installed host follows the upgrade flow with installation identity verification.
4. Choose the source, mode and installation options, then review the host and configuration.
5. Confirm and wait for the host operation receipt and health check. On failure, read the reason and logs in the operation history.
6. Return to sign-in after installation. User Mode and loopback-only installations can use a client-managed SSH tunnel.

## Release sources

| Source | Action and checks |
| --- | --- |
| Official stable | The host downloads the package and verifies the official ZIP digest and file inventory; an HTTPS catalog base can be specified |
| Local release bundle | Select and upload a ZIP from the client; suitable for an offline host |
| Bundle on the server | Browse or enter an absolute server ZIP path; the host reads that file directly |
| Custom HTTPS download | Supply the release ZIP HTTPS URL and its full 64-character hexadecimal SHA-256; the download digest is verified |

Local and server files still receive package-kind, architecture, required-file and extraction-safety checks. System Mode uses `*-server.zip`; User Mode uses `*-user-server.zip`. A client package cannot replace a server package. Actual availability follows the download catalog.

## Installation options

| Option | Scope and meaning |
| --- | --- |
| Mode | Linux System/User and Windows System; local Windows 10/11 management also offers Personal Mode by default. Remote Automatic follows preflight recommendations |
| Server port | 1–65535, default 5000; choose an available port |
| Program and data directories | System Mode allows separate roots; User Mode stores programs under its data root. Blank uses defaults; existing installations retain recorded roots |
| Configuration, state and cache directories | Linux User Mode allows all four XDG-related roots; use absolute paths that do not overlap |
| Network | System Mode offers loopback or LAN; The User Mode wizard defaults to loopback; LAN can be enabled with `listen-host`. LAN does not open the firewall automatically |
| TLS certificate | System Mode offers none, custom or self-signed. Select PFX/P12 with its password, or a PEM chain and private key. Self-signed names are comma-separated |
| File access | System Mode offers restricted, whitelist or full. Enter one absolute host directory per line for a whitelist |
| Administrator and root file access | Linux System Mode configures separate scopes and whitelists for both identities |
| Docker authorization | Explicit opt-in for Linux System Mode; off by default. Docker socket access is close to root authority |
| Other Linux systems | Explicitly allow a system outside the supported matrix; architecture, privilege and dependency checks remain |
| sudo credentials | Linux System Mode uses root or a sudo password; blank tries the current SSH password |

The client handles language, fixed actions, non-interactive execution, private staging paths, certificate password files and existing installation identity. No script arguments need to be entered. Certificate passwords are excluded from review.

Windows 10/11 Personal Mode uses fixed per-user program, data and deployment-history directories. Review network, TLS and privileged file scopes during installation. LAN listening does not automatically open the firewall; an explicit personal rule permits only LocalSubnet sources on Domain/Private networks.

## Update, repair and uninstall

Select an installed host in Server Center and open maintenance. Upgrade, repair and rollback continue with recorded directories; reinstalling retained data preserves installation identity. Ordinary upgrades and repairs retain TLS identity; certificate regeneration is an explicit choice.

Uninstall retains data by default. Permanently deleting databases, configuration, keys and logs requires separate confirmation. The client verifies host state afterward. Use operation logs and repair when the privileged helper is unavailable.

## First sign-in

After health verification, Windows 10/11 Personal Mode first authorizes the local device and then uses device-key sign-in. System-account and Alias sign-in apply to modes that support them. See [Windows 10/11 personal computers](/docs/en-US/latest/getting-started/windows).

After health verification, sign in with a host system account or a configured Alias. System accounts use host authentication; Alias uses an independent password hash. The server does not persist system sign-in passwords. Remembering credentials is an explicit local secure-storage choice.

## Next steps

- [Server Center](/docs/en-US/latest/apps/server-center)
- [Linux User Mode](/docs/en-US/latest/getting-started/user-mode)
- [Quick start](/docs/en-US/latest/getting-started/quick-start)
- [Sign-in and account security](/docs/en-US/latest/getting-started/login)
- [Windows 10/11 personal computers](/docs/en-US/latest/getting-started/windows)

[Remote installation](/docs/en-US/latest/getting-started/remote-installation) · [Update, uninstall and maintenance](/docs/en-US/latest/apps/server-maintenance)
