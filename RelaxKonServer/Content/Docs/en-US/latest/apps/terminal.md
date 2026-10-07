---
title: Terminal
description: Use a persistent remote terminal session.
category: Applications
order: 14
---

# Terminal

Terminal provides a remote PTY session over SignalR. The client renders terminal output locally; the server holds the PTY and preserves the session when the connection detaches.

## Walkthrough: identify execution host and user

1. Open Terminal in a signed-in Server workspace and check connection/session state. An SSH terminal targets the SSH host; a local terminal executes on the client.
2. On Linux, try `hostname`, `id` and `pwd`. On Windows, use `hostname`, `whoami` and your shell's current-directory command. Identify host, user and directory before acting.
3. Run ordinary commands in a test directory and inspect output/scrolling. With multiple sessions, identify the active session before typing.
4. Reconnect after a brief Server connection loss and inspect restored session state. Persistence does not promise recovery across every restart/sign-out or in local terminals; Windows Personal Server stops at sign-out.

![Screenshot placeholder：Capture: session state and host/user checks, explicitly showing Server, SSH or local mode; redact real accounts.](/assets/docs/screenshots/en-US/terminal-session.svg)

Check authorization before using an administrator terminal. Windows Personal's explicit administrator terminal runs as LocalSystem without the user's mapped drives; do not choose it by default for ordinary commands. Diagnose deployment receipts through [maintenance](/docs/en-US/latest/apps/server-maintenance), rather than repeatedly running installers.

## Overview

The terminal control is built on RoyalTerminal and embedded in a RemoteWindow. In remote mode the PTY runs on the server, the server is a **byte relay**, and VT rendering happens entirely on the client.

## Features

- Persistent session recovery: reconnect and return to the same shell state
- Local terminal rendering: scrollback, selection and fonts are client-side
- Input and size synchronisation
- Multiple sessions per user
- Automatic fallback to a local PTY when not signed in or unreachable

## How to use it

Open Terminal and start typing. The connection bar shows the session state; closing the window ends the session.

## Architecture

```text
TerminalControl (Client)
      |
SignalRTransport (ITerminalTransport)
      |
SignalR Hub /hubs/terminals  (JWT)
      |
TerminalHub → IPty (ConPTY / forkpty)
      |
Shell
```

## Security

- SignalR connections are authenticated with JWT and scoped to the current user
- The server relays bytes only; it does not inspect commands
- The session process runs as the signed-in host OS user

## Related documentation

- [Protocol and communication](/docs/en-US/latest/concepts/protocol)
- [Reconnect](/docs/en-US/latest/concepts/session)
