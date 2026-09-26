---
title: Quick start
description: Learn the desktop, windows and built-in applications in a few minutes.
category: Getting Started
order: 4
---

# Quick start

Once you are signed in you will see the RelaxKonOS desktop. Here is what people do first.

## Pick a connection mode

- **RelaxKonOS Server** opens the full desktop with your persistent workspace and the applications supported by that server.
- **SSH** opens a focused desktop for a confirmed SSH host. It includes Terminal, Server Center, SSH File Browser, Code Editor, and Image Viewer; it is not a RelaxKonOS workspace.

On a first SSH connection, check the displayed host-key fingerprint before choosing **Trust and connect**. See [Server Center](/docs/en-US/latest/apps/server-center) for the installation and maintenance workflow.

## Desktop and windows

- **Start menu**: click the button in the lower-left corner to list applications
- **Move a window**: drag its title bar; every edge supports 8-way resize
- **Minimise / maximise / close**: the buttons at the right of the title bar
- **Full screen and connection bar**: in full screen a connection bar appears and can be pinned or auto-hidden

## Open applications

Click any application in the start menu. Most are single-window (Settings, Task Manager, Docker, Firewall and others); Notepad and the code editor support multiple windows.

## Try this

1. Open **Terminal** and run `uname -a` or `ver`; output is rendered locally
2. Open **File Manager** and browse the host file system
3. Open **Task Manager** and watch the live performance graphs
4. Disconnect your network, then reconnect; the terminal session is still there

> Step 4 is the point. The session lives on the server; the client is only a view of it.

## Next steps

- [Applications](/docs/en-US/latest/apps/terminal)
- [Server Center](/docs/en-US/latest/apps/server-center)
- [Protocol and communication](/docs/en-US/latest/concepts/protocol)
