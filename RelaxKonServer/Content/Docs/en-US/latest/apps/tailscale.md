---
title: Tailscale Manager
description: Observe a remote host's devices and connection state, complete first login and connection changes with authorization, and keep the install in Component Center.
category: Applications
order: 44
---

# Tailscale Manager

Tailscale Manager manages the Tailscale node **on the server**, not a VPN on the machine you are sitting at. The client never talks to `tailscaled` directly: state reads, connection control and configuration changes all go through the server and a privileged helper, and only structured results come back.

## Pages

| Page | Purpose |
| --- | --- |
| Devices | Connection state, addresses and the tailnet device list |
| Connection and configuration | Connect, disconnect, and the two toggles for accepting DNS and advertised routes |
| First login | Start an official login on the server and hand the authentication page to this device's browser |
| Operations | Operation IDs kept per host and account, with original receipts, reconciliation and closure |

## Devices

The page is read-only: it reads the host CLI's `status --json` and shows the device name, DNS name, addresses and online state.

The tailnet device list needs **host network authorization** (resource `host/tailscale`). Without it the local state is still shown, but the peer list is hidden with an explanation rather than returned as a deceptively complete empty list.

Switching the UI language only redraws the state already loaded; it does not query the host again.

## Connection and configuration

There are exactly three changes: connect, disconnect, and change configuration. The configuration model is deliberately narrow — only "accept DNS" and "accept routes" — and accepts no native flags, auth keys or custom control servers.

Each change takes three steps:

1. Read the host's current connection state and configuration; actions stay unavailable until that read succeeds.
2. Preview: host, action, and the before/after values of DNS and routes.
3. Execute after an explicit confirmation.

The confirmation states the risk plainly: **this change can interrupt remote access, DNS or routes, including the session you are using; there is no automatic rollback.** Connecting requires the node to be logged in already; this application does not log you in as a side effect.

Execution goes through the shared host elevation entry point and asks for authorization at most once when the server explicitly requires it. An operation is deduplicated by `operationId`, operator, action, expected revision and configuration together: the same ID with different content is rejected, and a repeated submission only returns the original receipt.

## First login

The first-login page prepares a plan only after you click *Start login*, and executes only after success, explicit confirmation and a durable local save of the operation reference.

- The authentication page reaches this device's system browser **only when you click *Open official login page***. It is never opened automatically, copied to the clipboard, written to disk or placed in the operations list.
- Browser hand-off means "opening was requested". It does not mean the user authenticated, and it does not mean the daemon is logged in.
- A login challenge is bound to the original host, account and **original login session**: a new session after re-login cannot claim an old challenge, while the same account can still read the original operation and reconcile it.
- Closing the window only stops waiting; it **does not revoke a challenge that was already issued**.

Login results are reported as issued, native command completed, approval required, or unknown. When approval is required, approve the device in the admin console, then come back, refresh state and reconcile the original operation.

## Operations and recovery

Operation references live in the current OS user's local directory, isolated per host and account. They store identity references only — no authentication URL, configuration or result.

- When a response is lost the action is **not replayed**: reopening reads the server-side receipt for the original operation, the UI stays "awaiting verification" and changes stay disabled meanwhile.
- While an unresolved operation exists, new connect, disconnect, configure and login attempts are blocked, with a prompt to read the original receipt first.
- An unknown result leads to *Inspect recovery state*: after two consistent reads you can **close** the operation with fresh authorization and explicit confirmation.

Read the meaning of closure carefully: it accepts the host's **measured current state** and lifts the block caused by that operation. It does **not** retry, does **not** roll back, and does **not** rewrite the original operation as successful. The original result remains unknown; later healthy state never overwrites that fact.

## Install and uninstall

Installing is not done here; it belongs to the [Component Center](/docs/en-US/latest/apps/component-center):

| Platform | Source | Notes |
| --- | --- | --- |
| Linux | Signed official APT repository | Fixed package `tailscale`, CLI `/usr/bin/tailscale`, service `tailscaled.service` |
| Windows | Signed official MSI | Freezes `installer` metadata; the MSI is never unpacked as an archive |

Enabling the official repository on Linux requires explicit confirmation of the source and its key fingerprint; Component Center never overwrites a different source that the host already has. On Windows the privileged helper resolves the official release identity again, verifies the digest, the WinTrust publisher and the MSI product properties before running Windows Installer.

Uninstall keeps login and configuration data; install or update may restart the service and interrupt the current remote connection.

An existing external Tailscale on the host is **never adopted automatically**: Component Center reports it as external, and management rights require both its own ledger and verification evidence.

## Permissions and security

- Reading state needs an authenticated session; control, login and recovery additionally need host network resource authorization and administrator elevation.
- The privileged helper accepts fixed verbs only, rejects paths, arbitrary arguments and domain fields beyond the revision, and offers no generic command execution.
- Each command has a time and output limit; timeouts, oversize output and malformed output all fail, and raw JSON or low-level diagnostics never cross IPC.
- Authentication links are delivered through a dedicated short-lived channel only on click, and never enter state, logs, receipts or long-term caches.

## Platforms and limitations

| Platform | Status |
| --- | --- |
| Linux | Observation, control and first login verified on a real host (Ubuntu 26.04, Tailscale 1.104.4) |
| Windows | MSI install, cross-version changes, drivers and reboot recovery still need separate host acceptance |

- No supported version range is claimed: compatibility is established by real-host acceptance, not inferred from documentation.
- The UI exposes only "accept DNS" and "accept routes". Logout, auth keys, forced re-auth, exit nodes and subnet route advertisement are all outside this application.
- The mobile client is a **controller** over the same API and does not run a VPN runtime on the phone; its UI rules and device acceptance are tracked separately.
- Known CLI locations cover `/usr/bin/tailscale` and `Program Files/Tailscale`; other layouts are outside this slice.

## Related

- [Component Center](/docs/en-US/latest/apps/component-center)
- [Tunnel Manager](/docs/en-US/latest/apps/tunnel-manager)
- [Proxy Manager](/docs/en-US/latest/apps/proxy-manager)
- [Security model](/docs/en-US/latest/concepts/security)
