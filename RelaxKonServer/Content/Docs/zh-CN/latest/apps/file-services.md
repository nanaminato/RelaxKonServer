---
title: 文件服务
description: 由受管任务驱动的宿主文件共享服务，例如 SMB。
category: 应用程序
order: 36
---

# 文件服务

文件服务（File Services）把宿主系统上的文件共享能力（首轮为 SMB）纳入统一的受管安装与运维流程。

## 目标

- 通过统一的受管任务启动、配置与恢复服务
- Linux 侧使用 Samba，Windows 侧使用 SMB Server
- 与工作区偏好保持一致，避免每台机器重复配置

## 状态

首轮 SMB 方案处于设计与落地阶段；长期规格明确了服务发现、生命周期与恢复策略。

## 相关文档

- [文件管理器](/docs/zh-CN/latest/apps/file-manager)
- [持久化](/docs/zh-CN/latest/concepts/persistence)
