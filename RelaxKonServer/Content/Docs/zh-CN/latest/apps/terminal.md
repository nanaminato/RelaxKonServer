---
title: 终端
description: 使用可持久化的远程终端会话。
category: 应用程序
order: 14
---

# 终端

Terminal 通过 SignalR 提供远程 PTY 会话。客户端在本地渲染终端输出，连接暂时断开时服务端仍会保持会话。

## 概述

终端控件基于 RoyalTerminal，嵌入 RemoteWindow。远程模式下 PTY 运行在服务端，服务端只做**字节中继**，VT 序列的渲染完全在客户端完成。

## 功能

- 持久会话恢复：断线重连后回到原来的 shell 状态
- 本地终端渲染：滚动、选择、字体渲染都在客户端
- 输入与尺寸同步
- 每位用户可使用多个会话
- 未登录或无法连接时回退到本地 PTY

## 使用方式

打开终端后直接输入命令。连接栏显示当前会话状态；关闭窗口会结束该会话。

## 架构

```text
TerminalControl (Client)
      |
SignalRTransport (ITerminalTransport)
      |
SignalR Hub /hubs/terminals  (JWT)
      |
TerminalHub → IPty (ConPTY / forkpty)
      |
Shell
```

## 安全

- SignalR 连接通过 JWT 鉴权，会话与当前用户绑定
- 服务端只转发字节，不解析命令内容
- 会话进程以登录用户在宿主 OS 上的身份运行

## 相关文档

- [协议与通信](/docs/zh-CN/latest/concepts/protocol)
- [重连](/docs/zh-CN/latest/concepts/reconnect)
