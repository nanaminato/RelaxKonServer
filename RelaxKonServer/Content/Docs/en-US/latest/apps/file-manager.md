---
title: File Manager
description: Remote file management through the server REST API and host OS permissions.
category: Applications
order: 12
---

# File Manager

> This page describes current source capabilities, not the feature inventory of a particular package. See [release notes](/releases/0.1.2) for published artifacts and dates; linked implementation records track verification.

File Manager (Explorer) lets you work with files on the **server host** as if they were local, but it is not a remote-desktop file browser.

## Walkthrough: find a directory and upload a file

1. Identify the connection: Server workspaces operate on host files; SSH desktops use SFTP. Check host/account before opening File Manager from Start.
2. Use the navigation tree or address bar to enter an allowed target, and check its path/list. Start in a test directory, not a system directory.
3. Create and enter a test folder. Check for same-name targets and confirm the offered conflict handling before uploading.
4. Upload a small local file, wait for transfer and commit, then refresh and verify name/size. 100% progress alone is not a completed commit.
5. Download the uploaded file and verify it opens with correct contents locally. Before rename, copy or move, check selected items and destination.

![Screenshot placeholder：Capture: navigation tree, address bar and file list with a test path, names, sizes and selected item.](/assets/docs/screenshots/en-US/files-navigation.svg)

> Capture: navigation tree, address bar and file list with a test path, names, sizes and selected item.

![Screenshot placeholder：Capture: upload progress and the refreshed target file with completion state, name and size; redact sensitive paths.](/assets/docs/screenshots/en-US/files-transfer.svg)

> Capture: upload progress and the refreshed target file with completion state, name and size; redact sensitive paths.

If reading is denied, inspect identity/path authorization instead of treating it as an empty directory. Linux User Mode is limited to the current Unix home; Windows Personal helper access follows the reviewed scope. Recheck selected items before deletion; this guide promises neither a recycle bin nor recovery. See [transfers](/docs/en-US/latest/apps/file-transfers) for interrupted large uploads.

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

File operations follow host identity and path permissions. Refused reads are shown as inaccessible rather than empty. In system mode, protected directories use precise file authorization and a fixed Helper; renew expired authorization. User mode stays within the current Unix home and cannot elevate. Do not solve access failures by running Server as root/administrator.

In system mode, read authorization for a protected folder lasts 5 minutes and is bound to the current sign-in token. It covers that folder and its descendants, so browsing subfolders or opening images within them does not require repeated administrator credentials during that period. A single-file grant covers only that file. Other folders are outside its scope; writes and deletions require separate authorization, and sign-out revokes the grant.

[Upload progress, resumption and cancellation](/docs/en-US/latest/apps/file-transfers)

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
