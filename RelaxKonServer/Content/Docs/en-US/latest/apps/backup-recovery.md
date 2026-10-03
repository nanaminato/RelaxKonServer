---
title: Backup and Recovery
description: Backup and Recovery
category: Applications
order: 55
---

# Backup and Recovery

> This page describes current source capabilities, not the feature inventory of a particular package. See [release notes](/releases/0.1.2) for published artifacts and dates; linked implementation records track verification.

## Three different operations

| Operation | Current boundary |
| --- | --- |
| Application revision rollback | Reuses an old image/configuration; does not undo volume or database migrations |
| Definition backup and restore | Server supports encrypted definition archives, manifest validation, read-only preflight and restoration to a new stopped instance |
| Volume/database recovery | Consistency adapters and real verification are missing; recovery must not be claimed |

## Definition restoration

1. Create a definition backup and wait for re-read, authentication-tag and digest checks. Only Verified manifests enter restoration.
2. Preflight the backup, keys, source/target installation, object versions and resource conflicts. Blocking conditions prevent submission.
3. Confirm explicitly to create a new stopped definition. Old host ports and site bindings are not inherited; existing instances, volumes and secrets are not overwritten.
4. Check configuration and supply available secrets/resources separately, then explicitly confirm startup or traffic switching.

Android integrates backup creation, manifests and read-only preflight, but not restore submission UI. Backups require an explicitly configured recoverable key domain. Copying secrets.json or encrypted objects does not establish decryption on another installation. Interrupted writes are not automatically replayed.

Sources: [shared recovery contract](https://github.com/nanaminato/RelaxKonOS/blob/master/docs/applications/RelaxKonOS.BackupRecovery.Contract.md), [Android recovery boundaries](https://github.com/nanaminato/RelaxKonOS/blob/master/Client/RelaxKonOS.Client.Android/docs/features/OperationsRecovery.md).
