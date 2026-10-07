---
title: Welcome to RelaxKonOS
description: What RelaxKonOS is, which problem it solves, and how it differs from remote desktop software.
category: Getting Started
order: 1
---

# Welcome to RelaxKonOS

Developers can start with [Develop and debug from source](/docs/en-US/latest/getting-started/development) for current dependency versions, environment setup and build steps.

> This page covers current source capabilities. See [release notes](/releases/0.1.2) for package contents and the linked implementation documents for verification status.

RelaxKonOS is a **cross-platform, cloud-native desktop operating environment**. It keeps the interface on the device in front of you and the workspace on the server, so the same workspace follows you across devices.

Unlike remote desktop tools, RelaxKonOS transfers **state and intent**, never desktop pixels.

## The core model

| Layer | Location | Responsibility |
| --- | --- | --- |
| Client | Your device | Desktop shell, window management, application UI, input, local rendering |
| Protocol | In between | Explicit REST endpoints and SignalR hub contracts |
| Server | Ubuntu / Windows Server | Identity, workspace, storage, sync, remote runtime and remote services |

> The server **never captures or generates a desktop image**. It manages the state and services that make a workspace durable.

## What RelaxKonOS is not

- Not RDP, VNC or screen streaming
- Not a web management dashboard in a browser
- Not a virtual machine framebuffer projected to a client

## Next steps

- [Installation](/docs/en-US/latest/getting-started/installation)
- [User Mode installation (Linux, no sudo)](/docs/en-US/latest/getting-started/user-mode)
- [Quick start](/docs/en-US/latest/getting-started/quick-start)
- [Client / server architecture](/docs/en-US/latest/concepts/architecture)
- Source repository and issues: [nanaminato/RelaxKonOS](https://github.com/nanaminato/RelaxKonOS)

## Client and account guides

- [Android phones and tablets](/docs/en-US/latest/getting-started/android)
- [System accounts, Alias and credential security](/docs/en-US/latest/getting-started/login)
- [Windows 10/11 personal computers](/docs/en-US/latest/getting-started/windows)
