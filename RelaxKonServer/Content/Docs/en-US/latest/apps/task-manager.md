---
title: Task Manager
description: Watch real-time performance metrics and processes of the host OS.
category: Applications
order: 26
---

# Task Manager

Task Manager (RemoteTaskManager) shows the real state of the **server host**, similar to Windows Task Manager or GNOME System Monitor.

## Performance tab

- CPU: total and per-core usage with bar graphs
- Memory: total, used and a bar graph
- Disk I/O and file systems
- Network throughput
- GPU usage (via nvidia-smi when available)
- Uptime

A single server-side sampler pushes data at **1 Hz** over SignalR (`/hubs/performance`) and keeps **60 seconds of history**.

## Processes tab

- Process list with filtering by name, PID or user
- Low-frequency sampling with paged queries
- End a process; insufficient privileges are reported instead of silently failing

## Cross-platform

Collection is abstracted behind `ISystemMetricsProvider`: Windows uses `GetSystemTimes` and `GlobalMemoryStatusEx`, Linux reads `/proc/stat`, `/proc/meminfo` and `/proc/[pid]/status`.

> Capabilities the host or the service identity does not support are **explicitly degraded** rather than filled with fabricated values.

## Related documentation

- [Protocol and communication](/docs/en-US/latest/concepts/protocol)
- [Security model](/docs/en-US/latest/concepts/security)
