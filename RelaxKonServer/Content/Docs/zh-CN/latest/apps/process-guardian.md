---
title: 进程守护
description: 配置守护程序，让关键进程长期稳定运行。
category: 应用程序
order: 30
---

# 进程守护

进程守护（Process Guardian）管理需要持续运行的程序。

## 概述

独立的 Guardian Agent 进程以较高权限运行，通过本机认证的命名管道 IPC 接收服务端指令，执行守护程序的启动、停止与重启。

## 功能

- 守护程序配置与保存
- 启动、停止、重启
- 受保护服务进程的健康监控
- 守护日志经 SignalR（`/hubs/guardian-logs`）广播到客户端

## 架构

```text
Client
  |
SignalR /hubs/guardian-logs
  |
RelaxKonOS.Server  (IProcessGuardianService)
  |
命名管道 IPC（本机认证）
  |
RelaxKonOS.Guardian.Agent
  |
WorkloadSupervisor → 受守护进程
```

## 权限与安全

- Agent 独立于 Server 运行，权限边界清晰
- IPC 为本机命名管道 + 认证，只有合法进程可连接
- 健康检查与原生服务适配（systemd / SCM）仍在演进中

## 相关文档

- [任务管理器](/docs/zh-CN/latest/apps/task-manager)
- [安全模型](/docs/zh-CN/latest/concepts/security)
