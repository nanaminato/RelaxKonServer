---
title: Installation
description: Prepare and install the RelaxKonOS client and server.
category: Getting Started
order: 2
---

# Installation

RelaxKonOS is made of a **client** and a **server**. The client runs on the device you use every day; the server runs on the machine that should keep your workspace alive.

## Requirements

- **.NET 10.0 SDK** or later
- Client operating systems: Windows 10/11, macOS, Ubuntu 20.04+
- Server operating systems: Ubuntu 20.04+ or Windows Server 2016+

## Run the server

```bash
cd RelaxKonOS.Server
dotnet run
```

For production, change `Jwt:Secret` in `appsettings.json` to a random string of at least 32 characters and terminate HTTPS at a reverse proxy.

## Run the client

```bash
cd Client/RelaxKonOS.Client.Desktop
dotnet run
```

The client opens a login window. Sign in with the credentials of a **host operating system** account.

## First sign-in

1. Open the client and wait for the login window
2. Enter the host OS credentials of the server machine
3. The desktop opens and your workspace is created and synchronised

> Identity is validated by the host OS (Windows LogonUser / Linux PAM + NSS). RelaxKonOS never stores your password.

## Next steps

- [Quick start](/docs/en-US/latest/getting-started/quick-start)
- [Security model](/docs/en-US/latest/concepts/security)
