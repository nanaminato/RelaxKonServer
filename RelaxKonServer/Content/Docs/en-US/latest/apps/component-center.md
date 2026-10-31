---
title: Component Center
description: Review host component ownership and trusted sources, then install, update, uninstall while keeping data, and recover operations.
category: Applications
order: 42
---

# Component Center

Component Center is the single entry point for host components. It answers "who installed the Nginx, Docker, Git, FRP, proxy runtime and Tailscale on this server, at which version, and from which source", and then performs install, update and keep-data uninstall once authorized.

It does not take over business configuration: sites, tunnels, containers, subscriptions and shares stay with their own applications.

## Where the boundary is

| Concern | Owner |
| --- | --- |
| Component binaries, versions, sources, install ownership and lifecycle | Component Center |
| Service start/stop, business configuration, logs and runtime diagnosis | The owning application (Web Server, Docker, Tunnels, Proxy, File Services, …) |

Applications only show current host component state plus a jump link under their own **Settings → Component dependencies**. They no longer offer install buttons, so one host never ends up with two install paths that do not know about each other.

## Pages

| Page | Content |
| --- | --- |
| All components | The catalog filtered to the host platform, with category, integration and sources |
| Installed | Components the host confirms exist, and their ownership |
| Updates | Listed only when managed ownership is established and the catalog confirms a newer version |
| Workloads | Application environments that reference components rather than being packages (for example the proxy server base environment) |
| Tasks and recovery | Batch drafts, frozen task plans, execution progress and operations awaiting recovery |
| Sources | The install sources each component declares for the current platform, and whether they are wired up |

The catalog only lists components that **declare support for the current host platform**: a Windows host does not show sing-box, Squid, Samba or Docker Engine. Search, installed and updates share the same rule; no component gets a special case by name.

## Catalog

| Component | Category | Platforms | Wired install sources |
| --- | --- | --- | --- |
| sing-box | Network services | Linux | System package manager (APT) |
| mihomo | Network services | Linux, Windows | System package manager (Linux), trusted archive (Windows) |
| frpc / frps | Network services | Linux, Windows | Trusted archive (each direction reported separately) |
| Nginx | Sites and reverse proxy | Linux, Windows | System package manager (Linux), trusted archive (Windows) |
| Squid | Sites and reverse proxy | Linux | System package manager |
| Docker Engine | System base | Linux | System package manager; official source not wired yet |
| Git | System base | Linux, Windows | System package manager (Linux); Windows only observes an existing Git |
| Samba | File sharing | Linux | System package manager |
| Tailscale | Network services | Linux, Windows | System package manager (signed official repository), native installer (Windows MSI) |

"Declared" and "wired" are different things: an official source can appear as *planned*, which means the intent exists but this build has no transaction behind it. In that case no install button is shown.

## Install ownership

| State | Meaning | Allowed actions |
| --- | --- | --- |
| Managed | Installed by RelaxKonOS and recorded in the durable ledger | Update, repair, roll back, keep-data uninstall (per component and platform) |
| External | Already present on the host, not installed by RelaxKonOS | Read-only; never adopted automatically |
| Not integrated | Listed in the catalog, install still belongs to the owning application | Explanation and jump link only |
| Unknown | Read failure, missing permission or incomplete evidence | Neither treated as absent nor offered for install |

On Linux an external system package can be **explicitly brought under management**, but only when identity verification holds on every point: dpkg status, program file ownership, `dpkg --verify` (configuration file changes only), service ownership and `dpkg --audit`. If any check fails the state stays *unknown* and is never downgraded to *not installed*. Registration grants lifecycle authorization only — it does **not** reinstall, take over business configuration or change service start/stop.

If a managed package is later upgraded by an external `apt`, Component Center keeps the original authorization and reports a *verified external change*, but it never treats the new version as the receipt of the original install.

## Change flow

1. **Read state** — `GET /api/v1.0/components/{componentId}/state` returns ownership, measured version, install mode, host platform, problem codes and pending operations.
2. **Freeze a plan** — `POST /api/v1.0/components/{componentId}/plan` pins the source, version (or archive `releaseId`), every artifact's URI, size and SHA-256, and the data-retention choice.
3. **Confirm and elevate** — the confirmation lists host, action, version, source, digest and related artifacts, then asks for host administrator authorization for this plan.
4. **Execute** — the plan is submitted unchanged to that component's installation ledger, reusing the same idempotency key, permissions and durable operation record.
5. **Observe and recover** — follow progress, request cancellation or reconcile the original operation in *Tasks and recovery*.

The constraints matter more than the steps:

- Execution uses **only the frozen plan**. A changed host revision, install identity or target version invalidates the plan and forces a new one.
- A system package that has entered the install phase **cannot be cancelled by killing the package manager**.
- Failure and interruption **do not roll the package version back**, and current service health is not accepted as proof that the original operation succeeded. Such records stay in the workspace awaiting reconciliation.
- Uninstall keeps business data and configuration by default; deleting data is a separate selection with its own confirmation.

## Package sources

Archive and file installs on Windows offer four sources, all handed to the same ledger:

| Source | Meaning |
| --- | --- |
| Server download | The server fetches from an approved release source and verifies the digest |
| Client download then upload | The client uploads; the reference is bound to the current operator and expires |
| Local file | Pick a local archive and enter that file's real version |
| Server file | Pick an existing archive on the server and create a file reference |

"Download it yourself" only opens the official release page for the selected version. After that the file still has to come back through one of the sources above; opening an official page never relaxes validation.

## Trusted sources

- **System packages** — use the host's already-authorized repositories and signature metadata, freezing the candidate plus every dependency's file name, URI, byte count and SHA-256. Component Center **never adds repositories or signing keys by itself**; when the host has not registered a source, a specific policy problem code is returned.
- **Trusted archives** — either the pinned version list in configuration or the official releases directory (Nginx, Mihomo, frpc/frps, Tailscale). Results are cached for 15 minutes and can be refreshed manually. Both the redirect path and manual download obey the same source rules.
- **Tailscale on Linux** — requires explicit confirmation of the Tailscale Inc. stable signed repository and its key fingerprint, and never overwrites a different source or key that the host already has.
- **Tailscale on Windows** — pinned official stable metadata plus SHA-256, verified against the WinTrust publisher, MSI product/upgrade codes and architecture before entering the install transaction.

A pinned version means "available in configuration", not "latest upstream". Neither the version catalog nor an "up to date" label is a security conclusion.

## Version checks and updates

Selecting a component reads host state and prepares version information automatically; the page shows the installed version and the versions the source offers, and you can re-check at any time.

"Updates" requires all of: ownership established as managed, the source confirming a genuinely newer version, and no pending recovery transaction on the host. A repository without updates, an unknown state or an unfinished transaction never produces an update claim.

On Linux a system package update only accepts a higher version than the installed one; the same version is not an update. Update and uninstall act on the whole frpc/frps package, and after an update tunnels must be restarted from the business entry point. Uninstall does not require choosing an archive target.

## Launch gating

Before an application opens, the client reads the state of the components it depends on. There are four outcomes:

| Outcome | Handling |
| --- | --- |
| Installed | Opens normally (a stopped service still counts as installed) |
| Not installed | Offers a jump to Component Center; the jump never installs anything |
| Recovery / busy | Sent to *Tasks and recovery* first, then the application opens |
| Unknown / no permission | Not treated as missing, and not used to push an install |

When one application relates to several components, those components are **alternatives**: Tunnel Manager is satisfied by either frpc or frps. If the host declares a component unsupported on that platform, the outcome is *not required* and the application opens, so a platform difference is never mistaken for a missing dependency.

## Permissions and security

- Reading requires `server.components.read`; planning and submitting require `server.components.manage`.
- Execution additionally requires host administrator elevation, granted per plan. Elevation is neither cached nor reused.
- Privileged execution accepts only closed-set component verbs; there is no generic command runner.
- External installations are never adopted automatically. Ownership comes from the ledger and verification evidence, not from a binary "looking official".
- Package changes, control operations and service recovery share the same native lock; a pending or corrupted package record blocks new changes.

## Known limitations

- Integration is per component and per platform: Windows Nginx archives and Windows Git currently only have one side of the lifecycle, and multi-version update, repair and rollback still belong to the owning application or are not wired yet.
- System package install, external updates, interruption recovery and the full `dpkg --verify` path still need real-host acceptance per platform. Passing automated checks is not a host deliverable.
- Batch drafts and frozen task plans are limited to components with a batch path (currently sing-box); ordinary single-component installs stay in their own panel outside *Tasks and recovery*.
- The mobile client shares the same component API but maintains its own UI rules; neither side's verification substitutes for the other.

## Related

- [Web Server Manager](/docs/en-US/latest/apps/web-server-manager)
- [Docker Manager](/docs/en-US/latest/apps/docker)
- [Tunnel Manager](/docs/en-US/latest/apps/tunnel-manager)
- [Proxy Manager](/docs/en-US/latest/apps/proxy-manager)
- [Tailscale Manager](/docs/en-US/latest/apps/tailscale)
- [Security model](/docs/en-US/latest/concepts/security)
