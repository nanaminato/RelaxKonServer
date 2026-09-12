---
title: Persistence
description: How the server stores workspaces, application state and host-level resources.
category: Concepts
order: 66
---

# Persistence

The server uses **two SQLite domains**: a business database organised by user and workspace, and a host-global database for machine-specific resources.

## Business database

EF Core with SQLite, reconciled at startup by incremental `CREATE TABLE IF NOT EXISTS` statements.

| Area | Content |
| --- | --- |
| Identity | Users, devices, authentication protection state and security events |
| Workspace | Preferences, terminal settings, browser settings, window layout |
| Application state | Bookmarks, history, private key/value settings, image mirrors |
| Platform resources | Git repositories, tunnel definitions, registry entries |

## Host-global database

Holds resources tied to **this machine**, using a hand-written versioned migrator (v1 to v7) with transactional guarantees:

- Certificates and renewal records
- Web server sites, configuration snapshots and operation logs

## In-memory state

Some state deliberately stays in memory because its meaning is "the current connection" rather than "durable data":

- Sessions and refresh tokens
- PTY processes

## Consistency strategy

- Schema changes are additive, never destructive rebuilds
- Host-level resources migrate explicitly by version
- Cache invalidation and finer synchronisation can be added later

## Related documentation

- [Workspace](/docs/en-US/latest/concepts/workspace)
- [Sessions and devices](/docs/en-US/latest/concepts/session)
