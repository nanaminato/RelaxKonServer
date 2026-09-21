---
title: Proxy Manager
description: Install, configure and run a proxy core on the host with TUN as the primary mode, without losing the management connection.
category: Applications
order: 39
---

# Proxy Manager

Proxy Manager installs, configures, starts, stops, monitors and upgrades a proxy core on the **current host** that RelaxKonOS manages.

The first officially supported engine is **Mihomo**. The module is built around an engine abstraction so other headless proxy cores can be added later, instead of being written as a manager for one specific engine.

## Overview

The client **never connects directly** to the proxy core's control interface. Every call goes through the RelaxKonOS server, and the control interface listens on the local host only. The controller secret stays on the server and is never returned to the client.

## Operating modes

| Mode | Description |
| --- | --- |
| TUN | **Primary mode.** Takes over traffic at the system level; the main shape for a server |
| Listener only | Provides listening ports for other software to point at |
| RelaxKonOS only | Scoped to RelaxKonOS traffic; a later stage |

Treating TUN as an optional advanced switch is the desktop-GUI approach this module deliberately avoids.

## When to use it

- A server needs host-wide outbound proxying rather than an environment variable for one program.
- The management connection must survive a proxy failure instead of locking you out.
- Subscriptions, configuration and runtime versions should be managed in one place with a rollback path.

## Current capabilities

- Runtime management: install, update, rollback, uninstall and integrity verification, with both managed and external runtimes.
- Configuration and profiles: raw configuration and a managed overlay are kept separate instead of forcing complex configuration into a structure.
- Subscriptions: validated and backed up before an update, rolled back on failure, and never echoed back as a full URL.
- Proxy groups and nodes: list groups and switch the selected node.
- Connections: list active connections and close them.
- DNS reported as its own state rather than buried in configuration text.
- Live logs over the shared streaming channel, without a second push mechanism.
- Management traffic protection: a management route snapshot is captured before TUN is enabled, and the plan verifies the management connection stays reachable after the route change.
- Recovery: a recovery marker is written before network changes; an unfinished activation detected at startup enters recovery evaluation instead of being ignored.
- Emergency disable: a separate action that disables TUN and restores networking. It is not the same as uninstalling the engine, and profiles are kept intact.

## Permissions and security

- **Two layers must both hold**: RelaxKonOS authorization and host OS privilege. An engine running with high privileges does not mean any RelaxKonOS user may control it.
- The privileged helper accepts only **fixed, strongly typed** privileged actions. It offers no general command execution entry point.
- Everyday start and stop should not trigger elevation: installation and authorization happen once, and later toggles need no approval.
- Dangerous actions (managing TUN, managing the runtime, executing recovery) require server authorization.
- Subscription URLs, authorization headers, the controller secret, proxy passwords and private keys never reach ordinary logs and all pass through sanitization.
- RelaxKonOS does not disable system protection or add global exclusions for the proxy core; when the operating system blocks a binary it reports diagnostics instead.

## Platform differences

| Platform | Runtime hosting | TUN | Auto-redirect |
| --- | --- | --- | --- |
| Windows | Hosted by a long-running privileged helper; no separate proxy service is registered | Supported | Not supported |
| Linux | An independent system service | Supported | Supported |

The UI shows options based on the platform capabilities the server reports, and does not decide the operating system on its own.

## Known limitations

- The "RelaxKonOS only" mode belongs to a later stage; TUN and listener-only are the current modes.
- Configuration is managed as raw configuration plus a managed overlay, so the first version does not offer graphical editing of every configuration key.
- A set of TUN scenarios is skipped where no suitable host environment exists; skipped runs do not count as passing, and platform evidence still has to be produced on the target hosts.
- Enabling TUN on Windows requires the privileged helper to be in place. It is not a plain switch, and the UI states the reason explicitly when it is unavailable.

## Related documentation

- [Docker Manager](/docs/en-US/latest/apps/docker)
- [Certificate Manager](/docs/en-US/latest/apps/certificate-manager)
- [Security model](/docs/en-US/latest/concepts/security)
