---
title: Security Model
description: Identity, permissions and the elevation boundary in RelaxKonOS.
category: Concepts
order: 64
---

# Security Model

> This page describes current source capabilities, not the feature inventory of a particular package. See [release notes](/releases/0.1.2) for published artifacts and dates; linked implementation records track verification.

Host OS identity and execution permissions remain authoritative, but system sign-in and Alias are different credential entry points. Windows system sign-in uses LogonUser; Linux uses PAM/NSS. Alias verifies an independent one-way password hash bound to the existing host user and Workspace. The server does not persist system sign-in passwords; explicitly remembered local credentials use OS secure storage or Android vaults.

## File and application authorization

Ordinary file operations follow the bound host identity, paths and OS permissions. App permission cannot replace OS permission. Protected directories use separate file authorization and narrow Helper contracts. Refused reads are shown as inaccessible, not empty. User mode is limited to the current Unix home and has no file elevation.

## Administrator authorization and Helper

In system mode, system-authenticated administrator/root sessions have eligibility checked dynamically and eligible non-file actions need no repeated password. Ordinary users and Alias sessions explicitly authenticate a selected valid administrator to obtain precise, short-lived, token-bound authorization. Cross-account Guardian/script actions still need explicit approval for that operation. File actions follow separate path authorization.

Server runs with minimal privilege; Helper accepts fixed, strongly typed, audited actions. Installation permissions, the service account's ability to invoke Helper and the current user's authority over a target are separate conditions. Docker socket access is an explicit system-install choice. User mode disables host management such as Docker, firewall, proxy and certificates. A missing or rejecting Helper causes a reported failure, not a high-privilege Server bypass.

## Credentials and diagnostics

Passwords, private keys and tokens must not enter logs, audit or diagnostics. Alias hashes differ from reversible secrets; persistent application secrets use controlled encryption. Local vaults and SSH credentials are not workspace-synced. Credential changes or deletion can revoke existing sessions.

HTTPS, validation, path normalization, authorization and auditing constrain operations together. Uploads, destructive actions and restoration retain separate confirmation boundaries. There is no generic arbitrary-command elevation interface.

[Sign-in and account security](/docs/en-US/latest/getting-started/login) · [File uploads and resumption](/docs/en-US/latest/apps/file-transfers)

[Authorization operations guide](https://github.com/nanaminato/RelaxKonOS/blob/master/docs/platform/RelaxKonOS.PrivilegedOperations.Operations.md)
