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

## Pick a server installation method first

| Method | Privileges | Notes |
| --- | --- | --- |
| [User Mode](/docs/en-US/latest/getting-started/user-mode) | No sudo | Runs the server under an ordinary Linux account, binds `127.0.0.1` only, writes nothing into system directories |
| System Mode | root / administrator | The one-command installer registers system services and the privileged helper, for multi-user production |
| Run from source | .NET 10 SDK | The steps below; suited to development and debugging only |

> **Note**: the steps below need the .NET SDK and run the source tree directly — they are **not** the installation path for end users. To deploy a server, read [User Mode installation](/docs/en-US/latest/getting-started/user-mode) or the website [downloads page](https://relaxkon.com/downloads) first.

## Host authorization and the privileged helper

The server **does not run as root or administrator**. Every operation that needs host privilege goes through a dedicated privileged helper that accepts only fixed, structured actions and offers no general command execution entry point. The following points decide which features are available after installation.

### Administrator rights are a precondition

A System Mode install creates a dedicated service account, makes the helper's publish directory and policy files unwritable by that account, and allows only the service account to call the helper with no arguments. As a result:

- **Discovery and read-only features** work as soon as the service process can read.
- **Features that change host state** (installing runtimes, writing system configuration, controlling system services, deploying certificates and so on) require RelaxKonOS to hold sufficient privilege. When it does not, the UI returns an explicit problem code and asks you to redeploy with more privilege, instead of failing silently.
- The client **never** collects sudo, administrator or service-account passwords and never joins request parameters into a shell command. The correct response to insufficient privilege is to reinstall or restart the server with the required rights.

### Docker access is an explicit choice

Control of the Docker daemon socket is close to root privilege, so installation does **not** add the service account to that group by default. If you deliberately want [Docker Manager](/docs/en-US/latest/apps/docker) to manage the local engine, add `--docker-access` explicitly during a System Mode install:

```bash
sudo deployment/bootstrap/install-relaxkonos.sh --mode system --bundle /path/to/release --docker-access
```

- The choice is written to a root-only policy file; if Docker is already installed, the installer grants access and restarts the server.
- If Docker is installed by RelaxKonOS later, the helper performs the same fixed authorization afterwards and marks the task as "restart required". Restart the server and refresh the Docker status.
- **Without that option, a Docker install refuses to run before modifying the host** rather than changing half the system and then failing.
- User Mode does not support Docker by default: even when the service account could reach the socket, it is reported as a root-equivalent risk and requires separate confirmation.

### What happens when the helper is unavailable

When the helper is missing, uninstalled, or refuses a call, the affected feature fails with a stable problem code that explains why — for example, elevation-dependent proxy or Docker operations cannot complete. **Running workloads are not interrupted** and saved configuration is not rolled back; whether the failure happened before or after touching the host is recorded, so you can fix the cause and retry.

## Run the server from source

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

You can also choose **SSH** in the login window to connect to a host before it runs RelaxKonOS Server. SSH mode uses a confirmed host key and offers a focused maintenance desktop; it does not create a RelaxKonOS workspace. Read [Server Center](/docs/en-US/latest/apps/server-center) before using it to install or maintain a server.

## First sign-in

1. Open the client and wait for the login window
2. Enter the host OS credentials of the server machine
3. The desktop opens and your workspace is created and synchronised

> Identity is validated by the host OS (Windows LogonUser / Linux PAM + NSS). RelaxKonOS never stores your password.

## Next steps

- [User Mode installation (Linux)](/docs/en-US/latest/getting-started/user-mode)
- [Quick start](/docs/en-US/latest/getting-started/quick-start)
- [Server Center](/docs/en-US/latest/apps/server-center)
- [Security model](/docs/en-US/latest/concepts/security)
- [Docker Manager](/docs/en-US/latest/apps/docker) and [Proxy Manager](/docs/en-US/latest/apps/proxy-manager), which need host authorization
- Source and issues: [nanaminato/RelaxKonOS](https://github.com/nanaminato/RelaxKonOS)
