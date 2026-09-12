---
title: File Manager
description: Remote file management through the server REST API and host OS permissions.
category: Applications
order: 12
---

# File Manager

File Manager (Explorer) lets you work with files on the **server host** as if they were local, but it is not a remote-desktop file browser.

## Overview

The interface is ported from Jaya File Manager. Every file operation runs through the server REST API (`/api/v1.0/files/*`). The server calls the file system as the host OS process and therefore **reuses the host user and its permissions** instead of introducing a second ACL.

## Features

- Navigation tree (lazy loaded), grid view, address bar, toolbar and status bar
- Browse, create folder, delete, rename, copy and move
- Upload and download
- File and folder properties, with editable POSIX permissions on Linux
- Open with the default application or "open with", driven by the extension declarations in each application manifest

## How to use it

Open File Manager from the start menu, double-click a folder to enter it, and double-click a file to open it with the declared application. Destructive actions such as delete ask for confirmation.

## Permissions and security

- Every operation runs as the **signed-in user** on the host OS
- Out-of-scope access is refused by the host OS; RelaxKonOS does not bypass it
- There is no separate elevation path; privilege escalation is always delegated to the host

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
- [Security model](/docs/en-US/latest/concepts/security)
