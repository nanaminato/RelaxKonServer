---
title: Terminal
description: Use a persistent remote terminal session.
category: Applications
order: 10
---
# Terminal

Terminal provides a remote PTY session through SignalR. The client renders terminal output locally; the server holds the PTY and preserves its session when the connection is detached.

## Features

- Persistent session recovery
- Local terminal rendering
- Resize and input synchronization
- Multiple sessions per user

## Security

Terminal connections are authenticated and scoped to the current user.
