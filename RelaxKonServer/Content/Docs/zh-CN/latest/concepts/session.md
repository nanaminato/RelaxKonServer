---
title: 会话与设备
description: 设备、会话与工作区三者之间的关系。
category: 概念
order: 56
---

# 会话与设备

## 设备（Device）

设备是连接到工作区的客户端实例。服务端记录设备的身份与最近连接情况，用于展示与管理多设备场景。

## 会话（Session）

会话是设备与工作区之间的一次实时连接：

- 由登录创建，由登出或超时结束
- 承载当前的令牌与连接上下文
- 可以同时存在多个（多设备并行）

## 生命周期

```text
设备启动
   ↓ 登录（宿主 OS 校验）
创建 Session
   ↓
附加到 Workspace
   ↓
交互 / 同步
   ↓ 断开
Session 结束（Workspace 保留）
   ↓ 重连
新 Session 附加到同一 Workspace
```

## 持久能力与会话解耦

终端会话、被守护的工作负载等**不随 Session 结束而终止**。它们运行在服务端，重连后重新附加即可。

## 相关文档

- [工作区](/docs/zh-CN/latest/concepts/workspace)
- [重连](/docs/zh-CN/latest/concepts/reconnect)
