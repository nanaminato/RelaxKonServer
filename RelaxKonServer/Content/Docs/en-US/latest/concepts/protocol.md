---
title: Protocol and Communication
description: REST endpoints, SignalR hubs and the shared contract layer.
category: Concepts
order: 58
---

# Protocol and Communication

Client and server communicate through an **explicit protocol**. Business code never issues raw HTTP or WebSocket calls; every exchange goes through a contract.

## What the contract contains

| Element | Purpose |
| --- | --- |
| DTOs | `sealed record` with `[JsonPropertyName]` for stable serialization |
| Route constants | `*ApiRoutes` remove scattered string literals |
| Hub interfaces | Strongly typed client interfaces plus Methods and Events constants |
| Serialization conventions | Consistent naming policy and type mapping |

## Two transports

### REST

For request/response operations: workspace preferences, file operations, Docker, certificates and Git. Endpoints live under `/api/v1.0/*`.

### SignalR

For continuous push and bidirectional streams:

| Hub | Purpose |
| --- | --- |
| `/hubs/terminals` | Terminal PTY byte stream |
| `/hubs/performance` | Performance metrics at 1 Hz |
| `/hubs/guardian-logs` | Guardian log broadcast |
| `/hubs/workspace` | Workspace state events |

## Why SignalR

- Built-in reconnect support (whether it is enabled is **decided per hub**; the terminal hub does not enable it today, so a dropped connection means reopening the terminal)
- Built-in JWT authentication via `AccessTokenProvider`
- Strongly typed hub contracts
- No need to maintain a separate bare WebSocket endpoint

## Rules

- All cross-boundary communication goes through the protocol assembly
- The protocol assembly has zero third-party dependencies
- The client proxy lives in the client; hubs and endpoints live in the server

## Related documentation

- [Client / server architecture](/docs/en-US/latest/concepts/architecture)
- [Sessions and devices](/docs/en-US/latest/concepts/session)
