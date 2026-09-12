---
title: RelaxKonOS 概览
description: 产品定位、核心概念与整体能力地图。
category: 概念
order: 50
---

# RelaxKonOS 概览

RelaxKonOS 是一个**云原生桌面操作系统环境**，面向个人服务器与小型团队服务器的桌面化管理。

## 一句话定位

界面留在设备上，工作空间留在服务端。

## 能力地图

| 层次 | 组成 |
| --- | --- |
| Client | Desktop Shell、Window Manager、Application Runtime、Application SDK |
| Protocol | REST 契约、SignalR Hub 接口、DTO 与序列化约定 |
| Server | 身份认证、Workspace、Storage、Sync、Remote Runtime、Compute 与各平台服务 |
| Host | 宿主 OS 的用户、权限与文件系统 |

## 核心概念

- **Workspace**：持久的用户上下文
- **Session**：设备与工作区之间的实时连接
- **Device**：连接到工作区的客户端设备
- **Application Model**：应用的声明、启动与生命周期
- **Remote Services**：运行在服务端的结构化能力

## 设计原则

1. **本地渲染**：交互延迟留在客户端
2. **显式契约**：所有通信经过 Protocol
3. **委托宿主**：身份与权限复用宿主 OS
4. **明确降级**：不支持的能力显式说明

## 相关文档

- [客户端 / 服务端架构](/docs/zh-CN/latest/concepts/architecture)
- [工作区](/docs/zh-CN/latest/concepts/workspace)
- [协议与通信](/docs/zh-CN/latest/concepts/protocol)
