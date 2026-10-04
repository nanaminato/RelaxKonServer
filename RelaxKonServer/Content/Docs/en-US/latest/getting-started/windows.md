---
title: Windows 10/11 personal computers
description: Install Personal or System Mode on Windows 10/11, authorize this PC and pair other devices.
category: Getting Started
order: 8
---

# Windows 10/11 personal computers

Windows 10/11 can run Server and the desktop Client on the same PC. Local management defaults to Personal Mode and also offers System Mode. Windows Server supports System Mode only.

This page describes current source support. Check [Downloads](/downloads) and [release notes](/releases/0.1.2) for available artifacts; historical packages may not include these capabilities.

## Choose a mode

| Mode | Identity and lifetime | Installation and maintenance |
| --- | --- | --- |
| Personal | Owned by the Windows account that starts installation; Server runs without elevation, starts at sign-in and stops at sign-out. Closing Client does not stop Server | Local management, without SSH; installation, update, repair, rollback and uninstall use UAC to configure a separate privileged helper |
| System | Server, Guardian and the privileged helper are system services and run without an interactive sign-in; intended for system services and multi-user hosts | Local installation requires administrator privileges; remote maintenance requires an elevated administrator SSH session |

The modes have independent programs, databases, installation identities and operation histories. They do not convert automatically and must use different ports when coexisting. Windows 10/11 System Mode follows Windows Server authorization rules; ordinary domain users are not automatically administrators.

## Install and authorize this PC

1. Obtain Windows desktop Client and Server packages matching the architecture. Open **Manage this PC** on the sign-in page and choose Personal Mode. No OpenSSH Server or existing Server sign-in is required.
2. Choose an official release, local ZIP or custom HTTPS download with SHA-256. Review the port, network, TLS and privileged file scope. Personal Mode uses fixed per-user directories and the `*-server.zip` package.
3. Approve UAC during installation. Using another administrator's credentials does not change the initiating user's ownership, data or sign-in startup entry. Canceling UAC or failing to install the helper fails deployment.
4. Wait for the operation receipt and independent health check. Connect through a loopback address on the same PC and select **Set up this Windows device**. Negotiate verifies the signed-in Windows account and registers the device public key without an account password or Windows Hello PIN.
5. Only the installation owner may perform Personal Mode local authorization; other Windows administrators cannot substitute for the owner. Subsequent sign-ins use device-key signatures. The owner can recover a lost local key through the same local identity entry.

Windows 10/11 System Mode allows a local administrator to set up or recover a device key through loopback Negotiate. Windows Server does not expose this workstation authorization entry. Personal Mode ownership does not grant access to a separate System Mode installation.

![Screenshot placeholder：Capture: Manage this PC and Personal/System selection, showing current user and mode.](/assets/docs/screenshots/en-US/windows-local-mode.svg)

> Capture: Manage this PC and Personal/System selection, showing current user and mode.

## Permissions, data and daily use

The owner has RelaxKonOS administrator capabilities for this installation without joining Windows Administrators. Ordinary files, terminals and Docker Desktop use the owner's environment and permissions. Personal Mode installs no Guardian system service and provides no cross-user Guardian. Application capabilities remain subject to actual platform detection.

Programs are under `%LocalAppData%\RelaxKonOS-Personal\program`, with sibling `data` and `deployment` directories for persistent data and operation receipts/diagnostics. The separate LocalSystem helper is in protected `%ProgramFiles%\RelaxKonOS-Personal\<SID>` and exposes no network helper endpoint.

File operations lacking ordinary permissions may use the helper, but remain limited to the reviewed scope: managed data by default, or an explicitly selected whitelist or all local disks. An explicit administrator terminal runs as LocalSystem and does not inherit the user's mapped drives or domain credentials. Enabling it grants system-level local command execution to the owner and paired devices. Daily privileged operations do not repeatedly prompt for UAC or store a Windows administrator password.

## Pair other devices

1. After local authorization and device-key sign-in, generate a pairing code.
2. Codes expire in 10 minutes and are single-use. Restarting Server invalidates unused codes.
3. A new desktop or Android client generates its own private key, registers the public key using the code and signs a challenge to sign in. Private keys stay on devices; Server stores only public keys. Pairing needs neither the owner's password nor an administrator password.
4. Paired devices share the owner's account and workspace without creating Windows users. The owner can list and revoke devices. Revocation invalidates that device's access and refresh tokens and disconnects its existing Hub connections; other devices and password sessions are unaffected.

For other devices, Server must listen on LAN and pairing addresses must use a reachable hostname or IP, rather than `127.0.0.1` or `localhost`. Configure HTTPS with a certificate covering that address. Personal installation and maintenance can explicitly add a firewall rule limited to LocalSubnet sources on Domain/Private networks. A Windows administrator configures other scopes. LAN access does not imply public Internet access.

![Screenshot placeholder：Capture: local authorization and pairing entry, showing result and device management; hide active codes.](/assets/docs/screenshots/en-US/windows-device-pairing.svg)

> Capture: local authorization and pairing entry, showing result and device management; hide active codes.

## Update, rollback and uninstall

Maintain the matching installation through local management. Update, repair, rollback and uninstall verify installation identity and request UAC when needed; refreshing status does not. Uninstall retains data by default and removes the personal helper and its firewall rules. Permanent data deletion requires separate confirmation.

Automated checks cover account isolation, pairing and revocation, desktop deployment requests and System Mode authorization regressions. Before release, verify on real PCs: another administrator approving UAC for an ordinary owner, LAN HTTPS and Android pairing, privileged files/terminals, sign-out and sign-in, rollback/uninstall and coexistence. See the source [personal computer guide](https://github.com/nanaminato/RelaxKonOS/blob/master/deployment/WindowsPersonalComputer.md) for implementation and verification scope.

## Related documentation

- [Installation](/docs/en-US/latest/getting-started/installation)
- [Sign-in and account security](/docs/en-US/latest/getting-started/login)
- [Server Center](/docs/en-US/latest/apps/server-center)

[Remote installation](/docs/en-US/latest/getting-started/remote-installation) · [Update, uninstall and maintenance](/docs/en-US/latest/apps/server-maintenance)
