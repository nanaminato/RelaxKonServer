---
title: Firewall
description: Manage UFW state, default policies and rules on a Linux server host.
category: Applications
order: 32
---

# Firewall

The Firewall application manages **UFW** on a **Linux server host**.

## Features

- Read whether the firewall is enabled
- List numbered rules
- Change the enabled state and default policies
- Add or delete structurally validated rules

## How to use it

Open it to inspect the current state. Changing the state, policies or rules requires non-root users to confirm once with **their own password** through PAM.

## Permissions and security

- Linux only; the application is not shown on Windows Server
- root sessions are not asked to re-authenticate
- Other users confirm every change once through PAM
- Rules are structurally validated, so arbitrary command injection is not possible

## Related documentation

- [Security model](/docs/en-US/latest/concepts/security)
- [Port forwarding](/docs/en-US/latest/apps/port-forwarding)
