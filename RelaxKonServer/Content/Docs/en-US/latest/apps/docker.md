---
title: Docker Manager
description: Manage the host Docker Engine: containers, images, stacks, networks, volumes, image mirrors and proxies.
category: Applications
order: 28
---

# Docker Manager

Docker Manager (RemoteDocker) manages the Docker Engine of the **server host** from the desktop.

It manages **one local engine only**: the client never connects to the Docker socket directly, stores no Docker credentials, and never exposes the daemon API to the network.

## Features

- Engine status detection and installation guidance
- Containers: view, start, stop, restart
- Images: list and pull
- Stacks: validate, deploy and stop Compose projects
- Networks and volumes: view and manage
- Network proxy: outbound proxy for the daemon layer and the build layer
- Image mirrors: per-user Docker Hub compatible registries

## Implementation

The server calls the `docker` CLI through `IDockerEngineService` and handles Compose orchestration through `IDockerComposeService`. Endpoints live under `/api/v1.0/docker/*`.

## Image mirrors

Image mirrors are configured **on the Docker Manager's mirror page** per RelaxKonOS account and are **never written into the host's global `daemon.json`**.

- You can keep several HTTPS, Docker Hub compatible registry hosts and select one of them or "default".
- With the default, pulls run unchanged and Docker uses its own default registry.
- With a mirror selected, the server rewrites a Docker Hub reference to that mirror before calling the Docker CLI.
- An image that already names an explicit registry (for example a third-party image with an organisation prefix) is not rewritten, so it cannot be sent to the Docker Hub mirror by mistake.
- The selection is stored on the server and is **per user**, so one user's choice never replaces another user's mirror.

## Network proxy

In an environment that can only reach the internet through a proxy, Docker needs **two independent** proxy configurations that use completely different host mechanisms, so the state is reported per layer instead of being merged into one "proxy enabled" switch:

| Layer | What it covers |
| --- | --- |
| Daemon layer | The daemon's own outbound traffic, such as pulling images |
| Build layer | Outbound traffic during a build |

- The proxy source can be a manually entered address or the listening port of the built-in proxy runtime; the built-in case is probed for reachability first and fails closed when unreachable, so the daemon is never pointed at a port nobody is listening on.
- The build layer passes the proxy value through the child process environment, so it **never appears on the command line** and other local users cannot read a credentialed address from the process list.
- A proxy address may embed a user name and password. It is encrypted at rest; the operator-facing UI returns the value unchanged (otherwise a round trip through the form would rewrite real credentials as a mask), while logs, audit records and diagnostics are always masked.
- The daemon layer is a **host-global** setting: the Docker daemon belongs to the machine rather than to a workspace, so the last authorised writer wins.
- Enabling or removing the daemon layer proxy restarts Docker and interrupts running containers, so confirmation is required before submitting.
- The same configuration can also be changed from the **Network** category in [Settings](/docs/en-US/latest/apps/settings); the two places edit one setting.

## Engine lifecycle

Starting, stopping and restarting the whole Docker engine is a **host-level operation** that interrupts every container on the machine, so it is separate from per-container actions and stop and restart require explicit confirmation.

These actions change only the current running state. They do not enable or disable start-on-boot and do not modify any other configuration file on the host.

## Permissions and security

- Operations run with the service process identity on the host OS
- Mutating operations need host privileges (for example the `docker` group or root)
- **Docker access is off by default**: control of the daemon socket is close to root privilege, so deployment does not automatically add the service account to that group; you opt in explicitly at install time (see [Installation](/docs/en-US/latest/getting-started/installation))
- No separate credential store is introduced; registry credentials are kept as references only
- Deleting, force stopping, privileged containers and mounting the Docker socket all require a second confirmation

## Platform differences

| Platform | Notes |
| --- | --- |
| Ubuntu | Managed over the local Unix socket; the daemon layer proxy is written as a systemd drop-in and requires a service restart |
| Windows 10/11 | Managed over the local named pipe. When Docker is unavailable the status button opens built-in guidance: check virtualisation, install and start Docker Desktop, pick the backend, and confirm Linux container mode |
| Windows Server | Manages an already installed runtime that passes capability detection; it does **not** install one automatically |

The Windows desktop path suits development, personal self-hosting and verification; unattended or production deployments should prefer a dedicated Linux host.

## Known limitations

- Only one local engine is managed; container orchestration clusters, multi-node proxies and the remote daemon API are not implemented.
- Installation, persisted stack history, the container terminal and streaming resource statistics are still in design; the UI does not claim they are available.
- This application is designed as a **single-instance tool for an administrator**: authorization is currently bounded by the signed-in user, and there is no server-side Docker read/write role separation for a shared deployment. Do not expose it as a multi-tenant container control plane, and do not distribute its sign-in credentials to untrusted users.

## Related documentation

- [Proxy Manager](/docs/en-US/latest/apps/proxy-manager)
- [Application Deployments](/docs/en-US/latest/apps/application-deployments)
- [Settings](/docs/en-US/latest/apps/settings)
