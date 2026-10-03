---
title: Server Center
description: Connect to an SSH host and install, inspect, or maintain a RelaxKonOS Server without treating SSH as a full workspace.
category: Applications
order: 51
---

# Server Center

> This page describes current source capabilities, not the feature inventory of a particular package. See [release notes](/releases/0.1.2) for published artifacts and dates; linked implementation records track verification.

Server Center keeps the connection and deployment workflows for a remote machine in one place. It supports both a full RelaxKonOS Server connection and a direct SSH connection; these are deliberately different modes.

## Choose the right connection

| Connection | What it provides | What it does not provide |
| --- | --- | --- |
| RelaxKonOS Server | Sign-in, a persistent workspace, synchronized preferences, and the server capabilities exposed to your account | A replacement for SSH administration of an arbitrary host |
| SSH | A focused desktop for the confirmed SSH host: Terminal, Server Center, SSH File Browser, Code Editor, and Image Viewer | A RelaxKonOS workspace or access to server-only applications such as Docker, Firewall, or Certificate Manager |

SSH is useful before a server exists and for maintaining a host. It does not turn an SSH endpoint into a managed RelaxKonOS Server.

## SSH trust and saved connections

Before the first SSH connection, the client shows the host key fingerprint. It is stored only after you explicitly choose **Trust and connect**; closing or cancelling the prompt changes nothing. Saved connection entries identify the host, port, and user. Any saved password is handled by the operating system's secure credential store, not by a RelaxKonOS workspace.

The SSH desktop follows the client system language. It does not read or overwrite the language preference of a RelaxKonOS workspace.

## Install and maintain a server

For a confirmed SSH host, Server Center can run a preflight check, read the host's status, and guide these operations:

- First installation or upgrade from a verified release package
- Repair, rollback, and uninstall
- Reading the final host-side operation receipt rather than assuming that a launcher exit code means success

Linux System Mode can use root or authenticated sudo; Linux User Mode uses an ordinary account without sudo. Windows System Mode requires an elevated administrator SSH session. The wizard covers sources, ports, directories, network, TLS, file scopes, Linux identity scopes and Docker authorization; see [Installation](/docs/en-US/latest/getting-started/installation).

Uninstall keeps server data by default. Removing data is an explicit destructive choice and requires confirming the server name.

## Security boundary

- Deployment happens only after explicit confirmation and preflight results are shown.
- SSH host-key verification protects the connection from silently accepting a different host.
- Server Center does not make an SSH connection a privilege-escalation channel. Features that modify host state still require the server's documented authorization model.

## Related documentation

- [Installation](/docs/en-US/latest/getting-started/installation)
- [User Mode installation](/docs/en-US/latest/getting-started/user-mode)
- [Terminal](/docs/en-US/latest/apps/terminal)
- [File Manager](/docs/en-US/latest/apps/file-manager)
- [Security model](/docs/en-US/latest/concepts/security)

[Android server center and separate verification status](/docs/en-US/latest/getting-started/android)
