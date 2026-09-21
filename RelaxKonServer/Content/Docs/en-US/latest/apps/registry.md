---
title: Configuration Registry
description: Browse and edit the configuration-shaped desired state the schema explicitly allows, isolated per user.
category: Applications
order: 47
---

# Configuration Registry

Configuration Registry is a built-in application for browsing the **configuration-shaped desired state the server schema explicitly allows** together with its sync state.

**It is not the host operating system registry.** It offers no entry point to the host Windows registry, arbitrary database tables, secrets, sessions or high-risk commands; paths are governed by a schema allowlist in code.

## Overview

The application uses a key tree on the left, a value table on the right and an editor at the bottom. You can create, modify and delete logical registry values in your own scope, and refreshing re-reads them from the server.

Synchronization, versions and restarts are internal implementation details and are not shown to users.

## When to use it

- A workspace-level configuration item should be inspected per user rather than edited as a file.
- A single, explicitly schema-constrained entry point for configuration is preferable to exposing arbitrary configuration text.

## Current capabilities

- Browsing by key: the client requests only the **direct subkeys and direct values** of the key currently open, and never pulls a whole subtree when a root key is expanded.
- Editing: create, modify and delete values in your own scope.
- Persistence: an in-memory registry on the server holds the current state, changes are batched into the database after a brief pending-sync window, and one more flush happens on a clean shutdown.
- Defaults: terminal appearance, desktop preferences, browser settings and window layout for a workspace are written straight into the registry through the default value of their own keys; the older configuration files take no part in reads, migration or write-back.

## Permissions and security

- Reads use the authenticated user identity as the tenant boundary; unauthenticated access, unknown scopes and other users' data are all unreachable.
- Only keys under the workspace scope can be created or deleted by a user; deleting a key also deletes its subkeys and values.
- Keys and values are isolated by the same ownership boundary, so two users' listings never contain each other's data.
- The application accesses no operating system registry.

## Platform differences

The application and its API share **one managed implementation** on Windows and Ubuntu, so there is no platform branch.

## Known limitations

- Sync, version and restart state are not shown; they are internal.
- Edit history, optimistic concurrency and cross-instance cache invalidation are not yet provided and belong to a later stage.
- It must not be treated as a general configuration panel: paths outside the schema allowlist are unreachable, and the presence of a UI does not open them up.

## Related documentation

- [Settings](/docs/en-US/latest/apps/settings)
- [Persistence](/docs/en-US/latest/concepts/persistence)
- [Workspace](/docs/en-US/latest/concepts/workspace)
