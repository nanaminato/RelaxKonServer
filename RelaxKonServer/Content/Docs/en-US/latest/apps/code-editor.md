---
title: Code Editor
description: Edit code files on the remote host with syntax highlighting and multi-encoding support.
category: Applications
order: 18
---

# Code Editor

The code editor edits code files on a RelaxKonOS Server host, with syntax highlighting and multi-encoding support. In an SSH desktop, it can also open, browse, and save code through SFTP after the host key has been confirmed.

## Features

- Syntax highlighting and a familiar editing experience
- Read and write with multiple encodings (UTF-8, GBK, Shift-JIS and others)
- Shares the encoding dialog with Notepad
- Multiple windows for working on several files

## How to use it

Open it from the start menu, or launch it via "open with" from File Manager using an extension declaration. In an SSH desktop, open a code file from the SSH File Browser instead. Saving writes back to the connected server or SSH host; it never writes a remote file to the client by default.

## Permissions and security

- Reads and writes run with the signed-in user's host OS permissions
- Paths outside that scope are refused by the host OS
- The editor introduces no separate file access channel

## Related documentation

- [Notepad](/docs/en-US/latest/apps/notepad)
- [File Manager](/docs/en-US/latest/apps/file-manager)
- [Server Center](/docs/en-US/latest/apps/server-center)
