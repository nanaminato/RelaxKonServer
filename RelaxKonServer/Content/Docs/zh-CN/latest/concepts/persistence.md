---
title: 持久化
description: 服务端如何保存工作区、应用状态与宿主级资源。
category: 概念
order: 66
---

# 持久化

RelaxKonOS 服务端采用**双域 SQLite 持久化**：业务库按用户与工作区组织，宿主级库保存与机器相关的资源。

## 业务库

使用 EF Core + SQLite，启动时以增量 `CREATE TABLE IF NOT EXISTS` 补齐结构。

| 领域 | 内容 |
| --- | --- |
| 身份 | User、Device、认证防护状态与安全事件 |
| 工作区 | 偏好、终端设置、浏览器设置、窗口布局 |
| 应用状态 | 书签、历史记录、应用私有 KV、镜像源 |
| 平台资源 | Git 仓库、隧道配置、注册表键值 |

## 宿主级库（HostGlobal）

存放与**这台机器**相关的资源，使用自写的版本化迁移器（v1~v7）与事务保证：

- 证书与续期记录
- Web Server 站点、配置快照与操作流水

## 内存态

以下内容刻意保留在内存中，因为它们的语义是「当前连接」而非「持久数据」：

- Session 与刷新令牌
- PTY 进程

## 一致性策略

- 结构变更加「增量补齐」，避免破坏性重建
- 宿主级资源走显式版本迁移
- 未来可加入缓存失效与更细的同步策略

## 相关文档

- [工作区](/docs/zh-CN/latest/concepts/workspace)
- [会话与设备](/docs/zh-CN/latest/concepts/session)
