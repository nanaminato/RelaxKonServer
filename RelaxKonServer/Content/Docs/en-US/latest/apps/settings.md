---
title: Settings
description: Manage desktop appearance, language and workspace preferences that follow you across devices.
category: Applications
order: 24
---

# Settings

Settings is the Windows 11 / GNOME style hub for workspace preferences.

## Walkthrough: change appearance and language

1. Open Settings from Start in the intended Server workspace. SSH desktop preferences are not Server workspace preferences.
2. Change supported theme/wallpaper options in Personalization and observe the desktop; no host file editing is needed.
3. In Time and Language select Chinese, English, Japanese or follow-system, and verify navigation/page text changes immediately.
4. Wait for preference persistence, sign out and back into the same workspace, and verify the selection. Check synchronization on another device in that workspace. Outbound proxy is shared Server configuration, not this kind of workspace preference.

![Screenshot placeholder：Capture: Personalization and Time/Language choices, showing selected theme/language and applied desktop changes.](/assets/docs/screenshots/en-US/settings-preferences.svg)

Check scope before changing outbound proxy. Docker daemon proxy changes restart Docker and affect running containers; they are not merely appearance preferences.

## Categories

| Category | Contents |
| --- | --- |
| System | System information, about and update status |
| Personalisation | Wallpaper, theme palette, desktop icons |
| Time and language | Time and date formats, language and region |
| Network | Adapters and time zone (read-only, from the host OS) and the shared outbound proxy |
| Applications | Default programs and application permissions |
| Image mirrors | APT, NPM and other mirror sources; **the Docker mirror has moved to Docker Manager** |
| Developer | Developer mode, application packages and the [Network Inspector](/docs/en-US/latest/apps/network-inspector) entry point |

## Outbound proxy

The outbound proxy under **Network** is **one shared configuration on the server**; every user sees the same settings. It affects only the built-in features that are explicitly ticked and **does not change the host operating system's system proxy**.

- **Source**: a manually entered address, or the listening port of the built-in proxy runtime. When the built-in source is selected the UI states whether it is actually listening.
- **HTTPS address**: when left empty the HTTP proxy address is reused.
- **Addresses that bypass the proxy**: separate multiple entries with `;` or `,`.
- **Scope**: ticked per feature, for example Docker daemon image pulls, Docker builds, image tag and version lookups, and built-in runtime downloads.
- Enabling or removing the Docker daemon layer proxy restarts Docker and interrupts running containers, so confirmation is requested first.

The same configuration is also surfaced on the **Network proxy** page of [Docker Manager](/docs/en-US/latest/apps/docker); both places edit one setting and there is no second source of truth.

## Preference persistence

Workspace preferences persist through `/api/v1.0/workspaces/{id}/preferences` to the **workspace**, so they are shared across devices. At sign-in `PreferencesSync` loads them automatically and wallpaper, taskbar tint and clock format take effect immediately. Edits are debounced by 300 ms before saving.

The outbound proxy is the exception to that rule: it is a **server-level** configuration shared by all users and does not follow the workspace.

### Language preference

For a RelaxKonOS Server connection, **Time and language** can use **Follow system**. It resolves Chinese to `zh-CN`, Japanese to `ja-JP`, and other system languages to `en-US`, then synchronizes that workspace preference across devices. An SSH desktop always follows the client system language and never reads or changes a server workspace preference.

## Permission boundary

Host OS level settings (time zone, network adapters) are shown **read-only**. RelaxKonOS does not modify host system configuration; this is the hard rule that privilege changes are delegated to the host OS.

## Related documentation

- [Docker Manager](/docs/en-US/latest/apps/docker)
- [Proxy Manager](/docs/en-US/latest/apps/proxy-manager)
- [Network Inspector](/docs/en-US/latest/apps/network-inspector)
- [Server Center](/docs/en-US/latest/apps/server-center)
- [Workspace](/docs/en-US/latest/concepts/workspace)

[Account settings, Alias and session security](/docs/en-US/latest/getting-started/login)
