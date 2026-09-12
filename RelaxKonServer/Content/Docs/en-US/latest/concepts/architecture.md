---
title: Client / Server Architecture
description: How local rendering and remote services work together.
category: Concepts
order: 52
---

# Client / Server Architecture

RelaxKonOS follows a **state-sync** model rather than a pixel-streaming model.

## Responsibilities

### Client

- Desktop shell, taskbar, start menu
- Window manager and modal dialogs
- Application UI and local rendering
- Input handling and keyboard routing

### Server

- Identity and sign-in
- Workspace lifecycle and devices
- Storage and synchronisation
- Remote runtime and remote services
- Platform services (Docker, firewall, certificates, web servers, Git, tunnels, proxy and more)

> The server **never captures or generates desktop images**.

## A typical interaction

```text
1. The user types in a client window
2. The client encodes the intent as a protocol message
3. The server performs the operation on the host OS
4. The server returns a structured result
5. The client renders it with native controls
```

## Why not pixel streaming

| Dimension | Pixel streaming | State sync |
| --- | --- | --- |
| Transferred | Image frames | State and intent |
| Text quality | Limited by resolution | Native, always crisp |
| Interaction latency | Includes encode and transport | Handled locally |
| Bandwidth | Tracks visual change | Tracks operation volume |

## Related documentation

- [Protocol and communication](/docs/en-US/latest/concepts/protocol)
- [Window manager](/docs/en-US/latest/concepts/window-manager)
