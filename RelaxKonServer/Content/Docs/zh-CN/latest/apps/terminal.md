---
title: 终端
description: 使用可持久化的远程终端会话。
category: 应用程序
order: 14
---

# 终端

Terminal 通过 SignalR 提供远程 PTY 会话。客户端在本地渲染终端输出，连接暂时断开时服务端仍会保持会话。

## 实操：确认执行主机与身份

1. 在已登录的 Server 工作区打开终端，先检查连接与会话状态。SSH 桌面终端连接 SSH 主机；本地终端执行在客户端，不能混为一谈。
2. Linux 可先运行 `hostname`、`id`、`pwd`；Windows 可运行 `hostname`、`whoami`，并用当前 shell 的目录命令查看位置。这些检查用于确认命令将作用在哪台机器、哪个用户、哪个目录。
3. 在测试目录运行普通命令，观察输出与滚动；有多个会话时先看清当前会话再输入。
4. Server 连接短暂中断后重新连接，核对恢复的会话状态。服务端会话持久化不等于任何重启、注销或本地终端都能恢复；Windows 个人模式注销会停止 Server。

![截图占位 · 待替换：截图位置：终端会话状态和主机／身份检查输出；明确 Server、SSH 或本地模式，遮盖真实账号。](/assets/docs/screenshots/zh-CN/terminal-session.svg)

需要管理员终端时先确认授权范围。Windows 个人模式的显式管理员终端以 LocalSystem 运行，不继承用户网络盘；不能为了普通命令方便而默认使用该身份。部署失败的回执请到[服务器维护](/docs/zh-CN/latest/apps/server-maintenance)查看，不用重复运行安装命令猜测结果。

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
- 会话进程以登录用户在服务器操作系统上的身份运行

## 相关文档

- [协议与通信](/docs/zh-CN/latest/concepts/protocol)
- [会话与设备](/docs/zh-CN/latest/concepts/session)
