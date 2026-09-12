---
title: Sessions and Devices
description: How devices, sessions and workspaces relate to each other.
category: Concepts
order: 56
---

# Sessions and Devices

## Device

A device is a client instance attached to a workspace. The server records its identity and most recent connection so multi-device scenarios can be presented and managed.

## Session

A session is one live connection between a device and the workspace:

- Created by sign-in, ended by sign-out or timeout
- Carries the current token and connection context
- Several can exist at once (parallel devices)

## Lifecycle

```text
Device starts
   ↓ sign-in (validated by the host OS)
Session created
   ↓
Attached to the workspace
   ↓
Interaction / synchronisation
   ↓ disconnect
Session ends (workspace remains)
   ↓ reconnect
New session attaches to the same workspace
```

## Durable capabilities are decoupled from sessions

Terminal sessions and guarded workloads **do not stop when a session ends**. They run on the server, and a reconnecting client simply re-attaches.

## Related documentation

- [Workspace](/docs/en-US/latest/concepts/workspace)
- [Protocol and communication](/docs/en-US/latest/concepts/protocol)
