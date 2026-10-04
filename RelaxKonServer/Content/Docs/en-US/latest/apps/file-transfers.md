---
title: File Uploads and Resumption
description: File Uploads and Resumption
category: Applications
order: 13
---

# File Uploads and Resumption

> This page describes current source capabilities, not the feature inventory of a particular package. See [release notes](/releases/0.1.2) for published artifacts and dates; linked implementation records track verification.

## Walkthrough: verify upload completion

1. Confirm remote directory and local source in File Manager. Keep the source available and unchanged.
2. Watch confirmed bytes, rate and stage. After interruption, check Server-confirmed offset/session state and use the client's resume entry. Expired sessions must restart.
3. After commit succeeds, refresh the target and check name/size, then open or download to verify. 100% indicates only the byte-transfer stage.
4. Cancel unwanted tasks using the client action and check its state. Do not start concurrent same-name uploads while the outcome is unknown.

![Screenshot placeholder：Capture: transfer and final commit state with confirmed bytes, rate, stage, cancel entry and completed result.](/assets/docs/screenshots/en-US/transfer-progress.svg)

> Capture: transfer and final commit state with confirmed bytes, rate, stage, cancel entry and completed result.

## Upload, resume and cancel

Large desktop and Android uploads use chunked sessions instead of loading a whole file into memory. Clients show transferred bytes, progress, rate and cancellation; small files may use a single request with an explicit limit.

1. Select the remote destination and local source, and confirm conflict handling.
2. The server creates an upload session and reports chunk size and confirmed offset.
3. After interruption, query the server offset before resuming. Local progress alone cannot establish success. Recovery requires a valid session and an available, unchanged source.
4. Commit separately after all bytes arrive. A same-volume atomic rename turns staging into the destination file. Upload progress reaching 100% does not establish a successful commit.
5. Cancellation abandons the session and cleans staging. Restart as instructed if a session expires or becomes invalid.

## Paths, permissions and space

Unreadable directories are not presented as empty. Protected paths require separate file authorization and a restricted Helper channel, without bypassing host permissions. Renew expired authorization; user mode remains limited to the current Unix home. The server checks declared size, free space and staging budgets. Resumable does not mean unlimited size or retention.

## Android and verification

Android stages selected documents that cannot be read randomly, then tracks transfers at application scope with foreground notifications. Process termination, background restrictions, revoked source access and real network switching have separate acceptance checks; completion under every background condition is not promised.

Downloads stream to disk. The chunked-session description concerns uploads and does not imply downloads share that protocol. SSH transfers use SFTP rather than Server upload endpoints.

Implementation and tests: [upload contracts](https://github.com/nanaminato/RelaxKonOS/blob/master/Shared/RelaxKonOS.Protocol/Files/UploadContracts.cs), [upload record](https://github.com/nanaminato/RelaxKonOS/blob/master/docs/architecture/RelaxKonOS.FileUpload.Design.md), [Android transfers](https://github.com/nanaminato/RelaxKonOS/blob/master/Client/RelaxKonOS.Client.Android/docs/features/FileTransfers.md).
