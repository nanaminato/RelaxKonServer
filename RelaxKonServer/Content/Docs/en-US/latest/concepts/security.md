---
title: Security Model
description: Identity, permissions and the elevation boundary in RelaxKonOS.
category: Concepts
order: 64
---

# Security Model

> This page covers current source capabilities. See [release notes](/releases/0.1.2) for package contents and the linked implementation documents for verification status.

Host OS identity and execution permissions remain authoritative, but system sign-in and Alias are different credential entry points. Windows system sign-in uses LogonUser; Linux uses PAM/NSS. Alias verifies an independent one-way password hash bound to the existing host user and Workspace. The server does not persist system sign-in passwords; explicitly remembered local credentials use OS secure storage or Android vaults.

## File and application authorization

Windows 10/11 Personal Mode binds ownership to the initiating user's SID. The owner has RelaxKonOS administrator capabilities for that installation while Server remains unelevated. Installation and maintenance use UAC to configure a separate LocalSystem helper; approval by another administrator does not change ownership. Elevated files remain within the reviewed scope, while an explicit administrator terminal grants LocalSystem command execution to the owner and paired devices. Only the owner can perform local authorization, device private keys remain on devices, and revocation invalidates tokens and disconnects Hub connections. Personal Mode has no cross-user Guardian and is unavailable on Windows Server. See [Windows 10/11](/docs/en-US/latest/getting-started/windows).

Ordinary file operations follow the bound host identity, paths and OS permissions. App permission cannot replace OS permission. Protected directories use separate file authorization and narrow Helper contracts. Refused reads are shown as inaccessible, not empty. Linux User Mode is limited to the current Unix home and has no file elevation.

## Administrator authorization and Helper

In system mode, system-authenticated administrator/root sessions have eligibility checked dynamically and eligible non-file actions need no repeated password. Ordinary users and Alias sessions explicitly authenticate a selected valid administrator to obtain precise, short-lived, token-bound authorization. Cross-account Guardian/script actions still need explicit approval for that operation. File actions follow separate path authorization.

Server runs with minimal privilege; Helper accepts fixed, strongly typed, audited actions. Installation permissions, the service account's ability to invoke Helper and the current user's authority over a target are separate conditions. Docker socket access is an explicit system-install choice. Linux User Mode disables host management such as Docker, firewall, proxy and certificates. A missing or rejecting Helper causes a reported failure, not a high-privilege Server bypass.

## Credentials and diagnostics

Passwords, private keys and tokens must not enter logs, audit or diagnostics. Alias hashes differ from reversible secrets; persistent application secrets use controlled encryption. Local vaults and SSH credentials are not workspace-synced. Credential changes or deletion can revoke existing sessions.

HTTPS, validation, path normalization, authorization and auditing constrain operations together. Uploads, destructive actions and restoration retain separate confirmation boundaries. Personal Mode's explicit administrator terminal has the LocalSystem execution authority described above and must not be treated as an ordinary user terminal.

[Sign-in and account security](/docs/en-US/latest/getting-started/login) · [File uploads and resumption](/docs/en-US/latest/apps/file-transfers)

[Authorization operations guide](https://github.com/nanaminato/RelaxKonOS/blob/master/docs/platform/RelaxKonOS.PrivilegedOperations.Operations.md)
