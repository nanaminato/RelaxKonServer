---
title: Process Guardian
description: Declare guarded workloads so critical processes stay running.
category: Applications
order: 30
---

# Process Guardian

Process Guardian keeps critical workloads alive.

## Overview

A separate Guardian Agent process runs with elevated privileges and receives instructions from the server over an authenticated local named pipe.

## Features

- Declarative workloads with persistence
- Start, stop and restart
- Health monitoring of protected server processes
- Guardian logs broadcast to clients over SignalR (`/hubs/guardian-logs`)

## Architecture

```text
Client
  |
SignalR /hubs/guardian-logs
  |
RelaxKonOS.Server  (IProcessGuardianService)
  |
Named pipe IPC (local authentication)
  |
RelaxKonOS.Guardian.Agent
  |
WorkloadSupervisor → guarded processes
```

## Permissions and security

- The agent is separate from the server, keeping privilege boundaries clear
- IPC is a local named pipe with authentication; only legitimate processes can connect
- Health checks and native service adapters (systemd / SCM) are still evolving

## Related documentation

- [Task Manager](/docs/en-US/latest/apps/task-manager)
- [Security model](/docs/en-US/latest/concepts/security)
