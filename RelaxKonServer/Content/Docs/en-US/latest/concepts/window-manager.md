---
title: Window Manager
description: Window lifecycle, z-order, modal dialogs and keyboard routing.
category: Concepts
order: 62
---

# Window Manager

The window manager simulates an operating-system level window system and is the foundation of the desktop experience.

## Structure

```text
WindowManager
     |
RemoteWindow
     |
Avalonia Control
```

## Responsibilities

- Create and close windows
- Move and 8-way resize
- Focus and z-order
- Minimise, maximise and full screen
- Taskbar state synchronisation
- Modal dialogs with owner-local masking

## Modal dialogs

`AppContext.ShowDialogAsync<TResult>(owner, title, contentFactory)` provides a reusable, nestable modal mechanism that returns any result type and masks only the owner window.

## Host window control

The desktop shell implements host-level window control: title bar dragging, 8-way resize, minimise / maximise / close, full screen, and an mstsc-style connection bar (full screen toggle, pinning and auto-hide, connection information, and close connection = sign out).

## Related documentation

- [Application model](/docs/en-US/latest/concepts/application-model)
- [RelaxKonOS overview](/docs/en-US/latest/concepts/overview)
