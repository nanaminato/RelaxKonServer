---
title: Update, uninstall and maintenance
description: Update, uninstall and maintenance
category: Applications
order: 57
---

# Update, uninstall and maintenance

Use this guide for an installed Server. Enter Server Center through a trusted SSH connection for remote hosts, or [Manage this PC](/docs/en-US/latest/getting-started/windows) locally on Windows 10/11. Read existing state before choosing an operation; reinstalling is not a substitute for diagnosis.

## 1. Identify the installation and schedule maintenance

1. Select the correct host and refresh preflight/state. Check installation identity, mode, current version, program/data roots, port and health.
2. Inform other users: update, repair and rollback may restart services and disconnect clients. Keep a recoverable SSH or local management route.
3. Back up important files, configuration and databases, and verify the backup can be read. Program rollback does not promise data or schema rollback.
4. Inspect unfinished operations and their receipts before submitting anything new. Run one maintenance action per installation at a time.

![Screenshot placeholder：Capture: installed-host state with identity, mode, current/previous versions, roots, port and health.](/assets/docs/screenshots/en-US/maintenance-status.svg)

> Capture: installed-host state with identity, mode, current/previous versions, roots, port and health.

| Goal | Action | Distinction |
| --- | --- | --- |
| Use a new Server bundle | Update/upgrade | Client is not updated at the same time |
| Repair the current installation/components | Repair | Does not select a new version |
| Restore the recorded previous program | Rollback | Not file, volume or database restoration |
| Remove Server for later reinstall | Uninstall, retain data | Data remains on the host |
| Permanently clear managed data | Uninstall, delete data | Program rollback cannot undo deletion |

## 2. Update Server

1. Open the install/update wizard on an installed host and confirm it recognizes the existing installation.
2. Choose official, local ZIP, server ZIP or custom HTTPS using [remote installation](/docs/en-US/latest/getting-started/remote-installation). Verify mode, architecture, target version and digest.
3. Review the existing identity and directories. Update retains recorded roots and configuration; ordinary update preserves TLS identity without silently generating a new certificate.
4. Confirm maintenance and observe transfer, execution and verification. A disconnect during restart does not by itself mean failure.
5. Reconnect or refresh the same host. Check a successful receipt, target version and health, then test sign-in, files and terminal. Update the Client separately when new client features are needed.

![Screenshot placeholder：Capture: update review and result showing existing identity, current/target versions, retained roots and verification.](/assets/docs/screenshots/en-US/maintenance-update.svg)

> Capture: update review and result showing existing identity, current/target versions, retained roots and verification.

## 3. Repair and certificate maintenance

Select the installed host and choose repair of the current installation after checking its roots and identity. Ordinary repair retains data and TLS. TLS identity changes only when explicitly regenerating the LAN self-signed certificate or supplying a new certificate. Certificate replacement restarts service; clients must verify and trust it again, and names must cover the real LAN IP or DNS name.

Windows Personal Mode maintenance uses UAC when configuring its separate helper; refreshing status does not. Linux System repair still needs root or usable sudo. Verify the target port and scope before opting into extra firewall rules. Network inaccessibility alone does not prove installation corruption.

![Screenshot placeholder：Capture: repair options and confirmation, including certificate regeneration, identities and optional firewall rules; hide secrets.](/assets/docs/screenshots/en-US/maintenance-repair.svg)

> Capture: repair options and confirmation, including certificate regeneration, identities and optional firewall rules; hide secrets.

## 4. Restore the previous program version

Refresh state and use restore-previous-version only if a previous version is recorded. The action is unavailable otherwise. Check the target version, complete backup and maintenance confirmation, then execute. Refresh receipt, version and health afterward and sign in to test everyday functions.

Rollback uses recorded directories and the target version's deployment engine. It does not restore historical databases, configuration, files or container volumes. Confirm separately whether the older program can read data written by the newer version.

![Screenshot placeholder：Capture: rollback confirmation with current/recorded previous versions, target installation and maintenance impact.](/assets/docs/screenshots/en-US/maintenance-rollback.svg)

> Capture: rollback confirmation with current/recorded previous versions, target installation and maintenance impact.

## 5. Uninstall and retain data

1. Recheck host and installation identity, close client sessions and back up needed data.
2. Choose uninstall Server. Retain data is the default; keep it for reinstalling or temporary removal.
3. Choose deletion only for permanent removal and confirm the server name as requested. Managed databases, configuration, keys and logs may be removed.
4. Read the final receipt and refresh host state to confirm no installation remains. A lost connection does not prove uninstall succeeded.
5. Record retained directories and verify retained identity/data locations on reinstall. Do not delete state files to imitate uninstall. Windows Personal uninstall also removes its helper and associated firewall rules.

![Screenshot placeholder：Capture: uninstall confirmation and completion showing retain/delete selection, server-name confirmation, receipt and uninstalled state.](/assets/docs/screenshots/en-US/maintenance-uninstall.svg)

> Capture: uninstall confirmation and completion showing retain/delete selection, server-name confirmation, receipt and uninstalled state.

## 6. Read operation history and failure logs

Select the matching record and inspect operation ID, action, stage, state and problem code. Desktop can refresh the selected operation from the host to read its authoritative result and deployment log. Android opens a detailed summary through the record's details action. Refresh the original operation after failure, interruption or unknown outcome; missing local success is not permission to submit duplicates.

| Problem | Handling order |
| --- | --- |
| Staging space/quota exhausted | Check staging volume and quota, keep logs with the operation ID, then free space |
| Privileged helper unavailable | Read installation state/problem code and repair components; do not elevate everyday Server operation |
| Missing uninstall engine | Check versioned deployment files and repair before retrying; merely deleting programs is insufficient |
| TLS error or unreachable LAN | Check address, names, listening and firewall; explicitly choose certificate repair if needed |
| Receipt/state disagreement | Keep operation ID and refresh host results; do not declare success before health verification |

Redact accounts, addresses, passwords, private keys, tokens, pairing codes and sensitive paths before sharing logs/screenshots. Placeholder images specify capture requirements and do not represent device acceptance evidence.

## Routine maintenance order

Check installation and health, then failed operations. For changes, schedule maintenance, back up, review and submit once. Verify receipt, version and health afterward, then test sign-in, [files](/docs/en-US/latest/apps/file-manager), [terminal](/docs/en-US/latest/apps/terminal) and required host-management capabilities.
