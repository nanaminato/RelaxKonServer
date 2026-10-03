---
title: Sign-in and Account Security
description: Sign-in and Account Security
category: Getting Started
order: 6
---

# Sign-in and Account Security

> This page describes current source capabilities, not the feature inventory of a particular package. See [release notes](/releases/0.1.2) for published artifacts and dates; linked implementation records track verification.

## System sign-in and Alias

System sign-in is verified by Windows LogonUser or Linux PAM. In system mode, users first sign in with a real system account and can create an independent Alias and password in Settings. Both credentials bind to the same host identity, User and Workspace. An Alias creates neither a new OS user nor extra file permissions.

Alias passwords are stored only as one-way hashes. The server does not persist system sign-in passwords. When users explicitly remember credentials, the desktop uses OS secure storage and Android uses local vaults. Those local credentials are not workspace-synced data.

## Manage sign-in methods

Account settings can query, create, rename, change the password of or delete an Alias; sensitive changes revalidate current credentials. Disabling direct system-account sign-in requires a valid Alias and changes only the RelaxKonOS sign-in policy, not SSH, SMB or the system account. Deleting an Alias revalidates the system password and restores system sign-in. Password and security-policy changes can revoke older sessions and require another sign-in.

## Modes and authorization

User mode accepts only the Unix account running Server, with no Alias or administrator authorization. In system mode, Alias sign-in does not inherit the administrator shortcut of system-password authentication. Ordinary users and Alias sessions explicitly authenticate a selected administrator. Cross-account Guardian and script actions still need explicit approval for that operation; file access follows separate path authorization.

Sources: [authentication implementation](https://github.com/nanaminato/RelaxKonOS/blob/master/RelaxKonOS.Server/Identity/LoginAuthenticationService.cs), [Alias management](https://github.com/nanaminato/RelaxKonOS/blob/master/RelaxKonOS.Server/Identity/AliasCredentialService.cs), [privileged operations](https://github.com/nanaminato/RelaxKonOS/blob/master/docs/platform/RelaxKonOS.PrivilegedOperations.Operations.md).

[User mode](/docs/en-US/latest/getting-started/user-mode) · [Security model](/docs/en-US/latest/concepts/security)
