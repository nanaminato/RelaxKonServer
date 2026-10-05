---
title: Web Server Manager
description: Discover local web servers, then integrate or manage them with an administrator's consent and verifiable configuration transactions.
category: Applications
order: 41
---

# Web Server Manager

Web Server Manager is the **entry and dispatch layer**: it discovers the web server instances on this host and routes work by provider, with Nginx as the first complete provider. Adding another provider does not change any caller.

## Overview

RelaxKonOS is not designed to ship its own Nginx, nor to be an Nginx control panel. Its position is: **RelaxKonOS discovers the local web servers and, with the user's authorization, monitors, integrates with or manages them.**

## Two management modes

| Mode | Meaning | Boundary |
| --- | --- | --- |
| Integrated | Integrates with an existing web server without owning it | Manages only the configuration fragments RelaxKonOS created; no install, upgrade or uninstall |
| Managed | Installed and fully managed by RelaxKonOS | Install, upgrade, start, stop, configuration, sites, uninstall |

A discovered unmanaged web server is only a **candidate**: RelaxKonOS does not touch its configuration automatically. It only becomes integrated after an administrator explicitly confirms.

## When to use it

- Nginx already runs on the server and you want to add sites without taking over the existing configuration.
- A domain needs a static front end, SPA fallback and several reverse proxy paths.
- Certificate issuance and certificate deployment should stay decoupled: the certificate manager does not know Nginx exists.

## Current capabilities

- Discovery from several sources: PATH, common install locations, system services and running processes.
- Read-only status: version, executable, main configuration and configuration directory.
- Configuration transactions: generate, back up, write a temporary file, test, commit, reload and roll back on failure.
- Site model where a static root and proxy routes are **not mutually exclusive types**: a static site fills only the root path, a proxy-only site uses one root route covering the whole site, and a combined site can use both.
- Certificate binding: a site can bind a certificate that already exists in Certificate Manager; private keys stay on the server.
- Automatic reload after renewal: once a certificate has been renewed, only RelaxKonOS-managed sites referencing it receive one safe reload, with no configuration change.
- HTTP-01 integration: when an existing web server already holds port 80, it merely exposes the challenge path, and RelaxKonOS does not take the port over.
- Permission granting: on Linux the worker can be granted read access to a static directory explicitly (read and traverse only, without changing ownership or granting write access).

## Permissions and security

- This is an **administrator-mode** feature for a single server's administrator and does not use fine-grained user or workspace authorization. Instances, sites and configuration snapshots are host-global resources.
- Discovery and read-only status work whenever the process can read; integrating, installing, writing configuration, reloading, starting or stopping and certificate deployment all require RelaxKonOS to run with sufficient privileges and otherwise return stable problem codes.
- Host authorization uses unified administrator authentication: system-authenticated administrators are checked dynamically; ordinary users and Alias sessions explicitly authenticate a selected administrator. Credentials never enter logs or arbitrary shell commands. Installation permissions and action authorization are separate; see [Sign-in and Account Security](/docs/en-US/latest/getting-started/login).
- An upstream address is not an arbitrary writable URI: credentials, control characters, unknown schemes and undeclared ports are rejected, and the address is checked again after resolution so the site form cannot become an internal network probe.
- Configuration transactions run serially per instance; when an external modification disagrees with the snapshot the transaction aborts and requires a re-read rather than overwriting the user's change.
- Deleting a site or uninstalling removes only files carrying an ownership marker whose hash still matches, and never deletes user directories recursively.

## Platform differences

- Target platforms are **Ubuntu 24.04 LTS** and **Windows Server 2016 or later**.
- Linux controls Nginx through system services or native commands. Managed Nginx on Windows uses an independent SCM service that continues running when Server or Helper stops.

## Known limitations

- **The Nginx integration is the current first-stage scope**: installation, upgrade, uninstall and the configuration transaction and site management have landed; the remaining providers have not.
- IIS, Apache and Caddy have an abstraction boundary only and are **not implemented**; the UI does not claim they are available.
- Issuance and the reload triggered after automatic renewal can still be automated further in later stages; capabilities that are not implemented are not promised.
- Combined site configuration, directory authorization and sensitive path restrictions are described exactly as implemented, with nothing promised ahead of time.

## Related documentation

- [Certificate Manager](/docs/en-US/latest/apps/certificate-manager)
- [Application Deployments](/docs/en-US/latest/apps/application-deployments)
- [Security model](/docs/en-US/latest/concepts/security)

## Independent component services and maintenance boundaries

Managed Nginx, Mihomo and FRPC/FRPS use independent SCM services on Windows. Managed FRPC/FRPS use independent systemd services on Linux, with one instance per FRPC profile. Stopping or uninstalling Server, Guardian or Helper does not automatically stop retained components. Server restores management state from persistent records after restart rather than an in-memory PID. External runtimes still require explicit authorization and are not automatically converted into managed services.

The default uninstall retains running components, configuration and ownership records; reinstalling at the original data location allows management to resume. Full data removal first cleans up managed SMB, Nginx, FRP and Mihomo. Cleanup failures or ownership conflicts preserve the program, Helper and data: inspect the failure receipt before proceeding. Integrated system Nginx, other sites and shared files remain protected by ownership checks. The uninstall interface now lets you select managed SMB, Nginx, FRP and Mihomo for removal, retaining all by default. Retaining any component keeps the entire data root and management records. Selecting full data deletion selects all four components; deselecting one cancels data deletion. Docker Engine, containers and volumes are outside these four cleanup options.

Service lifecycle, OS restart, retained-data uninstall and reinstall have been verified on isolated Windows/Linux hosts. Full GUI/API flows and upgrades between versions still require regression checks. See [implementation and acceptance](https://github.com/nanaminato/RelaxKonOS/blob/master/docs/services/RelaxKonOS.IndependentComponentServices.Progress.md) and [maintenance](/docs/en-US/latest/apps/server-maintenance).
