---
title: Docker Manager
description: Manage the host Docker Engine: containers, images, stacks, networks and volumes.
category: Applications
order: 28
---

# Docker Manager

Docker Manager (RemoteDocker) manages the Docker Engine of the **server host** from the desktop.

## Features

- Engine status detection and installation guidance
- Containers: view, start, stop, restart
- Images: list and pull
- Stacks: validate, deploy and stop Compose projects
- Networks and volumes: view and manage

## Implementation

The server calls the `docker` CLI through `IDockerEngineService` and handles Compose orchestration through `IDockerComposeService`. Endpoints live under `/api/v1.0/docker/*`.

## Permissions and security

- Operations run with the service process identity on the host OS
- Mutating operations need host privileges (for example the `docker` group or root)
- No separate credential store is introduced

## Related documentation

- [Process Guardian](/docs/en-US/latest/apps/process-guardian)
- [Security model](/docs/en-US/latest/concepts/security)
