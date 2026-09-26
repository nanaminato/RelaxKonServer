---
title: File Manager
description: Remote file management through the server REST API and host OS permissions.
category: Applications
order: 12
---

# File Manager

File Manager (Explorer) lets you work with files on the **server host** as if they were local, but it is not a remote-desktop file browser.

## Overview

The interface is ported from Jaya File Manager. In a RelaxKonOS Server workspace, every file operation runs through the server REST API (`/api/v1.0/files/*`), which reuses the signed-in host user's permissions instead of introducing a second ACL. An SSH desktop instead uses its dedicated SFTP file browser; it is limited to the confirmed SSH connection and does not call the RelaxKonOS Server API.

## Features

- Navigation tree (lazy loaded), grid view, address bar, toolbar and status bar
- Browse, create folder, delete, rename, copy and move
- Upload and download
- File and folder properties, with editable POSIX permissions on Linux
- Open with the default application or "open with", driven by the extension declarations in each application manifest
- Theme-aware vector icons that follow the light and dark appearance and stay sharp on high-density displays

## How to use it

Open File Manager from the start menu, double-click a folder to enter it, and double-click a file to open it with the declared application. Destructive actions such as delete ask for confirmation.

## Permissions and security

- Every operation runs as the **signed-in user** on the host OS
- Out-of-scope access is refused by the host OS; RelaxKonOS does not bypass it
- There is no separate elevation path; privilege escalation is always delegated to the host

### Directory readability is reported honestly

When a directory cannot be read because of insufficient permission the UI **says "no access" and explains why instead of presenting it as empty**. An empty listing therefore always means the directory really is empty, not that you cannot see its contents.

Reading a protected directory is enabled by an administrator granting read access (or by running the server under an identity that holds it), never by the client collecting or forwarding a password.

## Architecture

```text
Explorer UI (Client)
      |
  REST /api/v1.0/files/*
      |
RelaxKonOS.Server
      |
  System.IO as the host user
```

## Related documentation

- [File services](/docs/en-US/latest/apps/file-services)
- [Server Center](/docs/en-US/latest/apps/server-center)
- [Security model](/docs/en-US/latest/concepts/security)
