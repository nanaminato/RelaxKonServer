---
title: 协议与通信
description: REST 端点、SignalR Hub 与共享契约层的设计。
category: 概念
order: 58
---

# 协议与通信

RelaxKonOS 的客户端与服务端之间有一层**显式协议**。业务代码不直接调用 HTTP 或 WebSocket，一切交互都经过契约。

## 契约内容

| 组成 | 说明 |
| --- | --- |
| DTO | `sealed record` + `[JsonPropertyName]`，保证序列化稳定 |
| 路由常量 | `*ApiRoutes`，避免散落的字符串字面量 |
| Hub 接口 | 强类型客户端接口与 Methods / Events 常量 |
| 序列化约定 | 统一命名策略与类型映射 |

## 两种传输

### REST

适合请求 / 响应式操作：工作区偏好、文件操作、Docker、证书、Git 等。端点集中在 `/api/v1.0/*`。

### SignalR

适合持续推送与双向流：

| Hub | 用途 |
| --- | --- |
| `/hubs/terminals` | 终端 PTY 字节流 |
| `/hubs/performance` | 性能指标 1 Hz 推送 |
| `/hubs/guardian-logs` | 守护日志广播 |
| `/hubs/workspace` | 工作区状态事件 |

## 为什么选择 SignalR

- 内建重连机制（**是否启用由各 Hub 决定**：终端 Hub 当前未启用，断开后需要重新打开终端）
- 内建 JWT 鉴权（`AccessTokenProvider`）
- 强类型 Hub 契约
- 不需要再维护一套裸 WebSocket 端点

## 规则

- 所有跨端通信必须经过 Protocol 程序集
- Protocol 程序集零第三方依赖
- 客户端代理与 Hub 实现分别位于 Client 与 Server

## 相关文档

- [客户端 / 服务端架构](/docs/zh-CN/latest/concepts/architecture)
- [会话与设备](/docs/zh-CN/latest/concepts/session)
