---
title: 欢迎使用 RelaxKonOS
description: 了解 RelaxKonOS 是什么、解决什么问题，以及它与远程桌面软件的区别。
category: 开始使用
order: 1
---

# 欢迎使用 RelaxKonOS

RelaxKonOS 是一个**跨平台的云原生桌面操作系统环境**。它把「界面」留在你面前的设备上，把「工作空间」留在服务端，让同一个工作区可以在多台设备之间连续使用。

与远程桌面类工具不同，RelaxKonOS 传输的是**状态与操作意图**，而不是桌面像素。

## 核心模型

| 层 | 位置 | 职责 |
| --- | --- | --- |
| Client | 你面前的设备 | 桌面 Shell、窗口管理、应用 UI、输入处理、本地渲染 |
| Protocol | 两者之间 | REST 端点与 SignalR Hub 的显式契约 |
| Server | Ubuntu / Windows Server | 身份、工作区、存储、同步、远程运行时与远程服务 |

> 服务端**从不捕获或生成桌面图像**。它管理的是让工作区得以持久存在的状态与服务。

## RelaxKonOS 不是什么

- 不是 RDP / VNC / 屏幕串流
- 不是浏览器里的 Web 管理面板
- 不是把某个虚拟机画面投放到客户端

## 接下来

- [安装 RelaxKonOS](/docs/zh-CN/latest/getting-started/installation)
- [快速开始](/docs/zh-CN/latest/getting-started/quick-start)
- [客户端 / 服务端架构](/docs/zh-CN/latest/concepts/architecture)
