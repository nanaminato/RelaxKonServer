---
title: User Mode installation (Linux)
description: Install the RelaxKonOS server under an ordinary Linux account — no sudo, no changes to system directories.
category: Getting Started
order: 3
---

# User Mode installation (Linux)

**User Mode** is for "I just want a server running under my own Linux account." It shares the same Server and Guardian binaries as System Mode, but keeps every piece of persistent state inside that account's XDG directories: it creates no systemd system units, installs no always-on privileged helper, and does not touch PAM, sudoers, the firewall, or `/etc`.

## Know the three installation methods first

The RelaxKonOS server has three installation methods that do not overlap. This page covers **User Mode** only.

| Method | Privileges | Use it for | Package to download |
| --- | --- | --- | --- |
| **User Mode** | Ordinary account, **root refused** | An individual running a server under their own account | `*-user-server.zip` |
| [System Mode](/docs/en-US/latest/getting-started/installation) | root / administrator | Multi-user production, registered system services | `*-server.zip` |
| Developer Mode | .NET 10 SDK | Working on RelaxKonOS itself | Source repository |

> **Warning**: client and server packages are not interchangeable. User Mode only accepts a release bundle whose manifest declares `packageKind: "user-server"`.

### How User Mode differs from System Mode

| Aspect | User Mode | System Mode |
| --- | --- | --- |
| Runs as | Your own account | Dedicated service accounts |
| Service registration | None; the `relaxkon` command manages processes | systemd units or Windows services |
| Listen address | `127.0.0.1` only | Configurable: local / LAN / reverse proxy |
| Privileged helper | Not installed, `Privileges: disabled` | Installed and invoked through fixed sudoers rules |
| Host changes | XDG directories only | `/etc`, systemd, sudoers, firewall |
| Upgrade | `relaxkon upgrade`, rolls back automatically if readiness fails | Re-run the installer |
| Uninstall | `relaxkon uninstall` | The uninstall script shipped in the release bundle |

## Before you install

- An **ordinary (non-root) Linux account**. Both the installer and the lifecycle command reject root, so using `sudo` makes the install fail.
- System commands: `bash`, `realpath`, `stat`, `find`, `sha256sum`, and `flock` (usually shipped in `util-linux`). A missing `flock` fails the install immediately.
- For an online install from an HTTPS release URL: `curl` and `unzip`.
- A `*-user-server.zip` release bundle plus its published SHA-256.
- No systemd, no sudo, no root.

> **Tip**: the website [downloads page](https://relaxkon.com/downloads) lists the stable-channel file names, sizes, and checksums; an offline server only needs the server package copied over.

## Install step by step

### 1. Extract the release bundle

Do this as the target account, and **do not** add `sudo`:

```bash
unzip RelaxKonOS-<version>-linux-x64-user-server.zip -d RelaxKonOS-user-server
```

### 2. Run the installer

```bash
./RelaxKonOS-user-server/deployment/user/install-relaxkonos.sh \
  --mode user \
  --bundle ./RelaxKonOS-user-server
```

The installer then:

1. verifies the bundle is complete (`manifest.json`, `payload/`, `deployment/user/relaxkon`);
2. checks `schemaVersion` and `packageKind: "user-server"` in `manifest.json`;
3. refuses any bundle that contains symbolic links;
4. verifies every file with `sha256sum` and confirms the file inventory matches the contents exactly;
5. installs the version under `server/versions/<version>/`, switches the `server/current` symlink, and refreshes `bin/relaxkon`.

You can also install online from the official release URL. `--release-uri` must be HTTPS and a 64-hex SHA-256 is mandatory:

```bash
./deployment/user/install-relaxkonos.sh \
  --mode user \
  --release-uri https://<host>/relaxkonos/stable/<version>/linux-x64/server/<archive>.zip \
  --release-sha256 <64-hex-sha256>
```

### 3. Confirm where it installed

User Mode writes only inside that account's XDG directories, at `0700` / `0600`:

| Purpose | Default path |
| --- | --- |
| Programs and version directory | `${XDG_DATA_HOME:-$HOME/.local/share}/relaxkonos/` |
| Lifecycle command | `${XDG_DATA_HOME:-$HOME/.local/share}/relaxkonos/bin/relaxkon` |
| Configuration and secrets | `${XDG_CONFIG_HOME:-$HOME/.config}/relaxkonos/` (`appsettings.user.json`, `secrets/guardian.secret`) |
| Runtime state | `${XDG_STATE_HOME:-$HOME/.local/state}/relaxkonos/` (PID files, control socket, `install-state.json`, SQLite database) |
| Logs | `${XDG_STATE_HOME:-$HOME/.local/state}/relaxkonos/logs/{server,guardian}.log` |
| Download cache | `${XDG_CACHE_HOME:-$HOME/.cache}/relaxkonos/` |

The installer does **not** modify your `PATH` and never writes a system-wide executable.

## Start and verify

```bash
RELAXKON=""${XDG_DATA_HOME:-$HOME/.local/share}/relaxkonos/bin/relaxkon""

"$RELAXKON" start
"$RELAXKON" status
```

Expected output of `status`:

```text
RelaxKonOS User Mode is running (pid <n>, loopback 127.0.0.1:5000).
```

It does not merely check whether the process exists: it requests `/ready` over the per-user private control socket (`…/relaxkonos/run/server.sock`, mode `0600`). If the socket is not ready it reports that explicitly instead of claiming success.

Other commands:

```bash
"$RELAXKON" start --foreground   # run in the foreground to watch the output directly
"$RELAXKON" stop                 # stop the Server and the Guardian
```

The server listens on `http://127.0.0.1:5000`; the port can be overridden:

```bash
RELAXKONOS_PORT=5100 "$RELAXKON" start
```

### Connect from your own machine

User Mode binds the loopback interface only, so remote access goes through SSH local forwarding — point the client at the local address:

```bash
ssh -L 5000:127.0.0.1:5000 <user>@<server>
```

## Upgrade

```bash
"$RELAXKON" upgrade --bundle ./RelaxKonOS-<new-version>-linux-x64-user-server
```

An upgrade stops the service, installs the new version, starts it, and waits for readiness. **If the readiness check fails it switches back to the previous version**, so an upgrade never leaves you with a server that will not start.

> **Note**: the same version number cannot be installed twice. Use a new version number when upgrading.

## Uninstall

```bash
"$RELAXKON" uninstall
```

It stops the service first, then removes the data / config / state / cache directories.

> **Warning**: `uninstall` deletes the database, configuration, secrets, and logs together, and **cannot be undone**. Back up `${XDG_STATE_HOME:-$HOME/.local/state}/relaxkonos/` and `${XDG_CONFIG_HOME:-$HOME/.config}/relaxkonos/` first if you need to keep anything.

## Troubleshooting

| Symptom | Cause and fix |
| --- | --- |
| `User Mode must not be installed as root.` | You used `sudo` or switched to root. Re-run as an ordinary account. |
| `--mode user or --mode system is required.` | `--mode user` is missing, or `--mode system` was used without `sudo`. |
| `not a complete user-server bundle` | The extracted package is not a `*-user-server` bundle, or it is incomplete. |
| `bundle is not a user-server manifest` | The bundle's `manifest.json` is not `packageKind: "user-server"`. |
| `bundle file checksum verification failed` | The files are corrupt. Re-download and check the published SHA-256. |
| `another RelaxKonOS lifecycle operation is already running` | Another session is running a lifecycle operation and holds `…/relaxkonos/run/launcher.lock`. Wait for it. |
| `flock is required for safe User Mode lifecycle operations` | `flock` (`util-linux`) is missing. Install it and retry. |
| `status` says the process runs but the control socket is not ready | Read `logs/server.log`; usually the first start is still initialising, or the port is taken. |
| `version already installed: <version>` | That version is already present. Use a new version number, or `uninstall` first. |

## Read the source and report issues

The whole User Mode implementation is readable in the repository:

- Lifecycle command: `deployment/user/relaxkon`
- User Mode entry point: `deployment/user/install-relaxkonos.sh`
- System Mode installer (for comparison): `deployment/bootstrap/install-relaxkonos.sh`

Source code, issues, and pull requests all live at [nanaminato/RelaxKonOS](https://github.com/nanaminato/RelaxKonOS).

## Next steps

- [Quick start](/docs/en-US/latest/getting-started/quick-start)
- [Installation](/docs/en-US/latest/getting-started/installation)
- [Security model](/docs/en-US/latest/concepts/security)
