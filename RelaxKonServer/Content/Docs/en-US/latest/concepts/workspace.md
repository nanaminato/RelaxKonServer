---
title: Workspace
description: The persistent context shared by a user's devices and services.
category: Concepts
order: 54
---

# Workspace

A workspace is the server-managed context for user preferences, application state, storage and devices. It lets an experience resume without making a single client device the source of truth.

## What it contains

| Content | Description |
| --- | --- |
| User preferences | Wallpaper, theme palette, time and date formats, language, region, default programs |
| Application state | Terminal settings, browser settings, bookmarks and history |
| Storage | User-scoped persistent data |
| Devices | Records of client devices that have connected |
| Window layout | Desktop and window arrangement state |

## Workspace and devices

Several devices belonging to one user attach to the **same workspace**. Sign in on a new device and the preferences and state are simply there.

```text
             ┌──────────────┐
  Device A ──┤              │
             │  Workspace   │
  Device B ──┤  (persistent)│
             │              │
  Device C ──┴──────────────┘
```

## Difference from a session

A workspace **persists**; a session is a **temporary connection**. Closing a session never destroys the workspace.

## Related documentation

- [Sessions and devices](/docs/en-US/latest/concepts/session)
- [Persistence](/docs/en-US/latest/concepts/persistence)
