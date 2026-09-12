---
title: RelaxKonOS Overview
description: Product positioning, core concepts and the capability map.
category: Concepts
order: 50
---

# RelaxKonOS Overview

RelaxKonOS is a **cloud-native desktop operating environment** built for the desktop management of personal and small-team servers.

## In one line

Keep the interface on the device; keep the workspace on the server.

## Capability map

| Layer | Components |
| --- | --- |
| Client | Desktop shell, window manager, application runtime, application SDK |
| Protocol | REST contracts, SignalR hub interfaces, DTOs and serialization conventions |
| Server | Identity, workspace, storage, sync, remote runtime, compute and platform services |
| Host | The host operating system's users, permissions and file system |

## Core concepts

- **Workspace**: the durable user context
- **Session**: a live connection between a device and the workspace
- **Device**: a client instance attached to the workspace
- **Application model**: how applications are declared, launched and managed
- **Remote services**: structured capabilities that run on the server

## Design principles

1. **Local rendering** keeps interaction latency on the client
2. **Explicit contracts** mean all communication flows through the protocol
3. **Delegate to the host** for identity and permissions
4. **Degrade explicitly** instead of pretending an unsupported capability exists

## Related documentation

- [Client / server architecture](/docs/en-US/latest/concepts/architecture)
- [Workspace](/docs/en-US/latest/concepts/workspace)
- [Protocol and communication](/docs/en-US/latest/concepts/protocol)
