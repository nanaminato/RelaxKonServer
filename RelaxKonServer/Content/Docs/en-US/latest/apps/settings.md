---
title: Settings
description: Manage desktop appearance, language and workspace preferences that follow you across devices.
category: Applications
order: 24
---

# Settings

Settings is the Windows 11 / GNOME style hub for workspace preferences.

## Categories

| Category | Contents |
| --- | --- |
| System | System information, about and update status |
| Personalisation | Wallpaper, theme palette, desktop icons |
| Time and language | Time and date formats, language and region |
| Network | Adapters and time zone (read-only, from the host OS) |
| Applications | Default programs and application permissions |
| Image mirrors | APT, Docker, NPM and other mirror sources |
| Developer | Developer mode and application packages |

## Preference persistence

Preferences persist to the **workspace** through `/api/v1.0/workspaces/{id}/preferences`, so they are shared across devices. At sign-in `PreferencesSync` loads them automatically and wallpaper, taskbar tint and clock format take effect immediately. Edits are debounced by 300 ms before saving.

## Permission boundary

Host OS level settings (time zone, network adapters) are shown **read-only**. RelaxKonOS does not modify host system configuration; this is the hard rule that privilege changes are delegated to the host OS.

## Related documentation

- [Workspace](/docs/en-US/latest/concepts/workspace)
- [Persistence](/docs/en-US/latest/concepts/persistence)
