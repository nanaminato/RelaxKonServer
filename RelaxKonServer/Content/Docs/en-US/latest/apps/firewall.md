---
title: Firewall
description: Manage Linux UFW and Windows Defender Firewall state, default policies and rules.
category: Applications
order: 32
---

# Firewall

The Firewall application supports Linux UFW and Windows Defender Firewall on Windows 10/11 and Windows Server. Windows System Mode and Windows 10/11 Personal Mode use the privileged Helper.

## Features

- Read whether the firewall is enabled
- List numbered rules
- Change the enabled state and default policies
- Add or delete structurally validated rules

## How to use it

Open it to inspect the current state. Changing the state, policies or rules uses shared host-administrator authorization. Verified administrators do not repeat authentication; other users authenticate an administrator in the elevation dialog.

## Permissions and security

- Linux uses UFW; Windows uses native firewall interfaces
- root sessions are not asked to re-authenticate
- Other sessions use shared temporary host-administrator grants
- Rules are structurally validated, so arbitrary command injection is not possible

## Windows Firewall

- Read status, enable or disable the firewall, and change default inbound and outbound policies.
- State and default-policy changes apply to **Domain, Private and Public** profiles together. Differing profile defaults have no single displayed value.
- Create, edit and delete rules managed by this application. System, other application, Group Policy and deployment-helper rules are excluded from this list.
- Rules support allow/block, inbound/outbound, TCP/UDP/any, IP/CIDR addresses and port ranges. Any protocol requires any port; ports refer to the local port for inbound traffic and the remote port for outbound traffic.
- UFW reject and limit actions are unavailable. Rules apply to all three profiles; choose the required source and destination scope.
- Changes require shared host-administrator authorization and native Windows operations through the Helper. Missing Helper access or policy restrictions return an error.

Changes can interrupt your management connection. Administrators can manage other Windows rules with `wf.msc`. Personal Mode's installation wizard creates a separate LAN rule limited to Domain/Private profiles and LocalSubnet sources.

## Related documentation

- [Security model](/docs/en-US/latest/concepts/security)
- [Port forwarding](/docs/en-US/latest/apps/port-forwarding)
