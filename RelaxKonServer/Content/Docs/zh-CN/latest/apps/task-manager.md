---
title: 任务管理器
description: 查看宿主 OS 的实时性能指标与进程列表。
category: 应用程序
order: 26
---

# 任务管理器

任务管理器（RemoteTaskManager）参考 Windows 任务管理器与 GNOME 系统监视器，展示**服务端宿主 OS** 的真实状态。

## 性能页

- CPU：整机与每核占用、柱状图
- 内存：总量、使用量与柱状图
- 磁盘 I/O 与文件系统
- 网络速率
- GPU 占用（可用时通过 nvidia-smi）
- 运行时间

数据由服务端统一采样器以 **1 Hz** 通过 SignalR（`/hubs/performance`）推送，并保留 **60 秒历史**。

## 进程页

- 进程列表，可按名称 / PID / 用户过滤
- 低频采样与分页查询
- 结束进程（权限不足时会提示需在宿主 OS 提权）

## 跨平台

采集通过 `ISystemMetricsProvider` 抽象：Windows 使用 `GetSystemTimes` 与 `GlobalMemoryStatusEx`，Linux 读取 `/proc/stat`、`/proc/meminfo` 与 `/proc/[pid]/status`。

> 宿主或服务身份不支持的能力会**明确降级**，而不是显示伪造数值。

## 相关文档

- [协议与通信](/docs/zh-CN/latest/concepts/protocol)
- [安全模型](/docs/zh-CN/latest/concepts/security)
