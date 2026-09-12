---
title: 安全模型
description: RelaxKonOS 的身份、权限与提权边界。
category: 概念
order: 64
---

# 安全模型

RelaxKonOS 不重建一套身份与权限体系，而是**把身份和文件权限委托给宿主操作系统**。

## 三条主线

### 1. 身份委托宿主

- Windows：原生 `LogonUser` API，支持本地账户与域账户
- Linux：PAM + NSS
- RelaxKonOS 在宿主确认凭据后签发自己的令牌，**从不保存密码**

### 2. 权限沿用宿主

- 文件操作以登录用户在宿主 OS 上的身份执行
- 没有独立的 ACL 层
- 越权请求由宿主 OS 直接拒绝

### 3. 提权隔离

- 特权操作通过专用 Helper 进程执行
- 契约窄且经过审计
- 危险操作需要显式确认
- 宿主已有机制（sudo、UFW、systemd）时优先复用

## 分层防护

| 层次 | 措施 |
| --- | --- |
| 传输 | HTTPS、JWT |
| 协议 | 显式契约、输入校验 |
| 服务端 | 路径规范化、目录穿越防护、统一错误处理 |
| 渲染 | Markdown 转义，默认不信任 HTML |

## 相关文档

- [权限与提权](/docs/zh-CN/latest/concepts/permissions)
- [持久化](/docs/zh-CN/latest/concepts/persistence)
