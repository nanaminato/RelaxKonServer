---
title: Events and Alerts
description: Events and Alerts
category: Applications
order: 53
---

# Events and Alerts

> This page describes current source capabilities, not the feature inventory of a particular package. See [release notes](/releases/0.1.2) for published artifacts and dates; linked implementation records track verification.

The event and alert center has a persistent event ledger, aggregated alerts, cursor queries and basic action APIs. The desktop has a minimal read-only summary/list. Deployment and Compose terminal signals have initial integration; a center failure does not roll back a completed deployment.

## Reading and handling

- Check the target resource, state, severity, count and last observation time. An alert list does not prove the live health of every resource.
- Acknowledge, permitted manual closure, timed suppression and unsuppression have separate API authorization and auditing. Read permission is not action permission.
- The real-time Hub sends invalidation notices; REST supplies authoritative details. The minimal desktop UI does not include all detail, action and deep-link flows.

## Undelivered scope

Reliable persistent replay for deployments/Compose, complete certificate/Docker/tunnel sources, Guardian sequence/checkpoint, full desktop interactions and real fault exercises remain outstanding. Android operation observation and foreground notices do not establish reliable background alerts. The center is not yet a sole production monitoring or delivery guarantee.

Sources: [implementation progress](https://github.com/nanaminato/RelaxKonOS/blob/master/docs/applications/RelaxKonOS.EventAlertCenter.Progress.md), [Android operations and recovery](https://github.com/nanaminato/RelaxKonOS/blob/master/Client/RelaxKonOS.Client.Android/docs/features/OperationsRecovery.md).
