---
title: Tunnel Manager
description: Manage the desired state and runtime of FRP tunnels with frpc and frps running as separate processes.
category: Applications
order: 40
---

# Tunnel Manager

Tunnel Manager lets you expose local services to a remote endpoint without coupling the protocol implementation into the main process.

The core trade-off: **RelaxKonOS manages tunnels natively, but `frpc` / `frps` always run as separate processes**. Traffic only ever flows between the FRP processes; RelaxKonOS takes no part in forwarding data and never depends on a tunnel being alive to stay available.

## Overview

- **The database is the source of truth, the generated configuration is not.** Tunnels and server profiles are stored as validatable, auditable desired state, and the generated configuration file is only an artifact.
- **Modelled around a provider**, with FRP as the first implementation; adding another tunnel technology later requires no change in callers.
- The RelaxKonOS server, authentication, LAN access and repair paths **do not depend on a tunnel being alive**.

## When to use it

- A machine with no public entry point needs to expose one or more ports.
- You need to connect to a RelaxKonOS-managed server, your own server, or a third-party compatible one.
- You want both a RelaxKonOS-managed runtime and the FRP already installed on the operating system.

## Current capabilities

- Tunnel definitions stored as desired state and validated before being applied.
- Server profiles: host, port, authentication and transport details for the non-secret part; secret material is referenced inside the server only.
- Runtime management: download, verify, versioned install, start, stop, status, logs, upgrade and rollback. An upgrade switches a version directory pointer instead of overwriting an existing binary.
- External runtimes: detection plus an explicitly managed start, and **never taking over an unauthorised configuration or process**.
- Tunnel types: `tcp`, `udp`, `http` and `https` in the first version.
- Managed servers: a server hosted by RelaxKonOS, with configuration and diagnostics pages.
- UI: overview, tunnels, servers, runtime and logs, and security state.

## Permissions and security

- Secret material never leaks through the ordinary API, logs, the generated configuration download entry point, or the client.
- The runtime's executable path and command line come from server constants and structured arguments, and are **never joined into a shell string**.
- When the operating system blocks or quarantines a runtime binary, RelaxKonOS reports diagnostics only; it **never** disables antivirus software, adds global exclusions, packs or tampers with the binary to bypass security software.
- Mutating operations require the corresponding read and manage permissions and are audited.

## Platform differences

Windows, Windows Server and Linux are supported. The differences lie mainly in runtime process supervision and operating-system security-software behaviour, both encapsulated in the server.

## Known limitations

- This version **excludes** `STCP`, `XTCP`, P2P visitors, plugins, custom configuration fragments, arbitrary environment variables and arbitrary command-line pass-through; each needs its own secret, network-exposure and input model first.
- It **does not automatically expose RelaxKonOS itself to the internet.** That capability belongs to a separate remote-access / TLS / identity review and should be implemented as an explicit, revocable system tunnel.
- It **does not install or force-enable the server-side binary**; that is an optional later goal and does not block the client-side main path.
- Cloudflare Tunnel, Tailscale and others have an abstraction boundary only and are not implemented.
- The tunnel protocol is neither embedded nor rewritten, and RelaxKonOS takes no part in forwarding data.

## Related documentation

- [Port Forwarding](/docs/en-US/latest/apps/port-forwarding)
- [Proxy Manager](/docs/en-US/latest/apps/proxy-manager)
- [Security model](/docs/en-US/latest/concepts/security)
