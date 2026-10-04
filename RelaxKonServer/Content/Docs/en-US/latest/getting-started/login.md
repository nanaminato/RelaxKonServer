---
title: Sign-in and Account Security
description: Sign-in and Account Security
category: Getting Started
order: 6
---

# Sign-in and Account Security

> This page describes current source capabilities, not the feature inventory of a particular package. See [release notes](/releases/0.1.2) for published artifacts and dates; linked implementation records track verification.

## Windows 10/11 personal computers

The Windows 10/11 Personal Mode owner can select **Set up this Windows device** using loopback Negotiate on the same PC, without an account password or Hello PIN. Other Windows administrators cannot substitute for the owner. Subsequent sign-ins use device keys. Single-use pairing codes expire after 10 minutes and pair desktop or Android devices with the same owner account and workspace. Revocation invalidates that device’s tokens and Hub connections. Windows 10/11 System Mode permits local administrators to set up or recover device keys; Windows Server does not expose this workstation entry. See [Windows 10/11 personal computers](/docs/en-US/latest/getting-started/windows).

## System sign-in and Alias

System sign-in is verified by Windows LogonUser or Linux PAM. In system mode, users first sign in with a real system account and can create an independent Alias and password in Settings. Both credentials bind to the same host identity, User and Workspace. An Alias creates neither a new OS user nor extra file permissions.

Alias passwords are stored only as one-way hashes. The server does not persist system sign-in passwords. When users explicitly remember credentials, the desktop uses OS secure storage and Android uses local vaults. Those local credentials are not workspace-synced data.

## Sign in to Server through an SSH tunnel

The desktop and Android Server sign-in pages support SSH tunnel connections. Enter the SSH host, port and account, with a password or PEM/OpenSSH private key and optional passphrase. Set the Server address to a loopback service on that SSH host, such as `http://127.0.0.1:5000`. Verify the fingerprint on first connection or when the host key changes.

SSH and Server credentials can be entered and securely saved separately, or you can explicitly choose the same username and password for password authentication. Connection tests release their tunnel when finished. A signed-in session uses an automatically assigned local loopback port and closes its tunnel on sign-out or session expiry. Saved profiles can be selected again; sensitive credentials are not workspace-synced.

This connects to a full Server workspace, distinct from the terminal/SFTP connection in a separate SSH desktop. Jump hosts, reverse-proxy subpaths and automatic background reconnection are currently unsupported; sign in again after a disconnection. HTTPS still verifies certificate names, validity and trust chains; the certificate must match the requested `127.0.0.1`. Device sleep and mobile network switching still require device verification.

## Manage sign-in methods

Account settings can query, create, rename, change the password of or delete an Alias; sensitive changes revalidate current credentials. Disabling direct system-account sign-in requires a valid Alias and changes only the RelaxKonOS sign-in policy, not SSH, SMB or the system account. Deleting an Alias revalidates the system password and restores system sign-in. Password and security-policy changes can revoke older sessions and require another sign-in.

## Modes and authorization

Linux User Mode accepts only the Unix account running Server, with no Alias or administrator authorization. In system mode, Alias sign-in does not inherit the administrator shortcut of system-password authentication. Ordinary users and Alias sessions explicitly authenticate a selected administrator. Cross-account Guardian and script actions still need explicit approval for that operation; file access follows separate path authorization.

Sources: [authentication implementation](https://github.com/nanaminato/RelaxKonOS/blob/master/RelaxKonOS.Server/Identity/LoginAuthenticationService.cs), [Alias management](https://github.com/nanaminato/RelaxKonOS/blob/master/RelaxKonOS.Server/Identity/AliasCredentialService.cs), [privileged operations](https://github.com/nanaminato/RelaxKonOS/blob/master/docs/platform/RelaxKonOS.PrivilegedOperations.Operations.md).

[User mode](/docs/en-US/latest/getting-started/user-mode) · [Security model](/docs/en-US/latest/concepts/security)
