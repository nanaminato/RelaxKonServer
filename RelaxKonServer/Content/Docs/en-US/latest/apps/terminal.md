---
title: Terminal
description: Use a persistent remote terminal session.
category: Applications
order: 14
---

# Terminal

Terminal provides a remote PTY session over SignalR. The client renders terminal output locally; the server holds the PTY and preserves the session when the connection detaches.

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
