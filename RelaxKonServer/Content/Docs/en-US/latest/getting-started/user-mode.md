---
title: User Mode installation (Linux)
description: Install the RelaxKonOS server under an ordinary Linux account — no sudo, no changes to system directories.
category: Getting Started
order: 3
---

# User Mode installation (Linux)

Linux User Mode runs the server under your ordinary account, without sudo, system services or a privileged helper. It defaults to `127.0.0.1`; remote access uses a client-managed SSH tunnel. To listen on `0.0.0.0`, write `0.0.0.0` to `listen-host` in the user configuration directory (normally `~/.config/relaxkonos`, respecting `XDG_CONFIG_HOME` or `RELAXKONOS_USER_CONFIG_ROOT`) and restart with the user launcher. The client installation wizard still defaults to loopback.

## Install through Server Center

1. Get the client from [Downloads](/downloads), connect to the Linux host through Server Center and verify its host key.
2. Use a non-root account for preflight and explicitly choose **Linux User Mode** in the wizard.
3. Choose official stable, a local ZIP, a server-side ZIP or a custom HTTPS download. User Mode requires `*-user-server.zip`; a system package cannot substitute.
4. Set the port (default 5000) and optionally the data, configuration, state and cache directories. Blank uses the XDG defaults below.
5. Review and confirm. Wait for `/ready` verification and the final receipt, then sign in through the client-managed SSH tunnel.

Availability follows the download catalog. Obtain a suitable User Mode package if one is not published yet. The host requires Bash, Python 3, curl, unzip, realpath, stat, find, sha256sum and flock; systemd is not required.

## Directories and permissions

| Purpose | Default path |
| --- | --- |
| Programs and versions | `${XDG_DATA_HOME:-$HOME/.local/share}/relaxkonos/` |
| Configuration and keys | `${XDG_CONFIG_HOME:-$HOME/.config}/relaxkonos/` |
| State, database and logs | `${XDG_STATE_HOME:-$HOME/.local/state}/relaxkonos/` |
| Cache | `${XDG_CACHE_HOME:-$HOME/.cache}/relaxkonos/` |

Use absolute directories that do not overlap. Private directories and files use `0700` / `0600`. Host-side directory discovery lets the client maintain the actual installation after reconnecting.

User Mode installs no PAM or sudoers configuration, changes no system firewall and disables privileged host features such as Docker management. Networking defaults to loopback and can be configured for LAN; system TLS and privileged file scopes do not apply.

## Update and uninstall

Select the host in Server Center and use maintenance for upgrade, repair, rollback or uninstall. No separate SSH client or manual lifecycle command is needed. A failed readiness check triggers recovery; inspect the final receipt. Uninstall retains data by default; permanent deletion requires separate confirmation.

Port conflicts, missing dependencies, wrong package kind or architecture, checksum failures and operation locks block installation. Check preflight and operation history, resolve the cause and retry.

See [Installation](/docs/en-US/latest/getting-started/installation) for all source, directory and maintenance options, and [Server Center](/docs/en-US/latest/apps/server-center) for entry points.
