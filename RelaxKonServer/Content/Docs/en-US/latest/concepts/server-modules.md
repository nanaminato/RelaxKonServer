---
title: Server modules
description: Server features are enabled, kept resident or released per module, and how modules differ from components and applications.
category: Concepts
order: 68
---

# Server modules

The RelaxKonOS server is not one monolithic program. Core owns startup, authentication, workspaces, base storage, protocol and recovery; everything else is registered as a **module**, which can be enabled on demand and released from memory when idle. Clients only see what the server can do right now, not how modules are loaded.

## Modules, components and applications

| Object | What it is | Managed by | Does enabling a module install it? |
| --- | --- | --- | --- |
| Server module | A RelaxKonOS server feature unit such as Terminal, Git or Docker management | Component Center → Server modules | — |
| Component | Host software such as Nginx, Mihomo, Docker Engine or FRP | [Component Center](/docs/en-US/latest/apps/component-center) | No |
| Application | A window and UI on the client, such as the terminal, file manager or Docker page | Each application | No |

The boundary only runs one way: enabling the Docker module does not install Docker Engine, and disabling or sleeping a module neither stops an installed Docker, removes containers, nor clears business configuration or user data. Software installs, updates and removals always go through the component's managed flow. When a module is enabled but its software is missing, the UI should surface the entry and take you to Component Center instead of treating "module enabled" as "feature available".

## Official modules

Nine official modules are compiled into the host. The configuration name is derived from the module ID, so `relaxkonos.system-performance` becomes `Modules:SystemPerformance`:

| Module | Module ID | Configuration name |
| --- | --- | --- |
| Terminal | `relaxkonos.terminal` | `Terminal` |
| Firewall | `relaxkonos.firewall` | `Firewall` |
| System Performance | `relaxkonos.system-performance` | `SystemPerformance` |
| Git | `relaxkonos.git` | `Git` |
| Tailscale | `relaxkonos.tailscale` | `Tailscale` |
| Docker | `relaxkonos.docker` | `Docker` |
| Certificates | `relaxkonos.certificates` | `Certificates` |
| Web Server | `relaxkonos.webserver` | `Webserver` |
| Proxy | `relaxkonos.proxy` | `Proxy` |

Core is always present: startup, authentication, host mode, authorization, base storage, workspaces, protocol, the Helper channel and the recovery entry points belong to no optional module.

## Enabling a module

- The switch is the configuration key `Modules:<name>:Enabled`, for example `Modules:Git:Enabled=false`. **A restart applies it**; the module set of a running process is frozen at startup.
- With no `Modules` configuration at all, every module is enabled, matching a server that predates the registry. Unknown module names, non-boolean values or unknown settings stop startup instead of quietly changing the set.
- Signed-in clients can read the inventory at `GET /api/v1.0/server/modules`, which reports each module's effective state, dependencies, deactivation blockers and residency. **Reading it never wakes a module.**
- Deactivation checks dependencies and reverse dependencies first: when another enabled module still depends on it, or unfinished work and recovery duties remain, you get a blocker instead of a silent cascade.

## Residency and on-demand loading

Releasing a module only means **freeing memory**. It does not delete files, change configuration, touch business data or stop external services.

| Policy | Behaviour |
| --- | --- |
| Keep loaded | Stays resident after the first load |
| Release when idle (default) | Waits out a grace period, 300 seconds by default, after the last lease ends |
| Try to release after sign-out | Checks the modules that session used as soon as it signs out; if others still use them, waits for the last lease |

- Waking happens on a real business request: authentication, authorization, effective enablement and platform checks **pass first**, then the module lease is acquired. Unauthenticated or unauthorized requests never trigger a load, and ordinary requests never download modules, enable modules or install software.
- Concurrent first access to one module is merged into a single load. Waking has a 30-second limit and a queue cap; timeouts and overload return a stable temporary-unavailable error, and requests with possible side effects are never replayed.
- Git, Firewall and Tailscale currently declare a releasable object graph, at the "objects / caches / connections" level. Assembly unloading and separate process exit are not delivered, so no immediate working-set drop is promised.
- Load failures keep a stable reason: `module-load-failed`, `module-wake-timeout`, `module-wake-overloaded`, `module-host-stopping`. The UI shows the reason and a retry entry instead of reporting success.

## Where to manage them

- **Desktop client**: Component Center → Server modules shows the inventory, dependencies, residency state and health checks. Enable, disable, wake, try-to-sleep and residency policy changes all produce a preview plan first, and module package uploads happen on the desktop as well.
- **Android**: the same entry lists modules and submits the same plan-based actions, without module package uploads.
- **Settings → System** keeps only a read-only summary and a link to Component Center.

## Current state and what a release contains

- The module catalog and the enable switch entered the source tree on 2026-10-28. The published 0.2.0-v1a (2026-10-04) **predates** that work, so the inventory and residency states described here appear only after upgrading to a build that includes modularisation.
- Third-party modules use the standalone `.rkmodule` package with source trust, privilege confirmation and a separate module host. Windows and Linux have platform acceptance records; the authoritative contracts live in the product repository under `docs/architecture/RelaxKonOS.ServerModule*.md`.
- This page describes current source capability. Whether a published package contains a given capability is decided by that version's release record, not by what the source supports.

## Related documentation

- [Client / server architecture](/docs/en-US/latest/concepts/architecture)
- [Application model](/docs/en-US/latest/concepts/application-model)
- [Component Center](/docs/en-US/latest/apps/component-center)
- [Terminal](/docs/en-US/latest/apps/terminal)
