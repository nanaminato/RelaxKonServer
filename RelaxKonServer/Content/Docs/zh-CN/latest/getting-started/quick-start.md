---
title: 快速开始
description: 用几分钟时间了解桌面、窗口与内置应用的基本操作。
category: 开始使用
order: 4
---

# 快速开始

## 图文使用教程

- [第一次连接服务器](/docs/zh-CN/latest/getting-started/tutorial-connect)
- [熟悉桌面与窗口](/docs/zh-CN/latest/getting-started/tutorial-desktop)
- [用文件管理器和终端完成第一次操作](/docs/zh-CN/latest/getting-started/tutorial-files-terminal)

> 本页对照当前源码说明能力，不是某个发布包的功能清单。已发布产物与日期见[发行说明](/releases/0.1.2)，功能验收状态以所链接的实现记录为准。

登录成功后你会看到 RelaxKonOS 桌面。下面是最常见的几件事。

## 选择连接模式

- **RelaxKonOS Server** 会打开完整桌面，其中包含持久工作区与该服务端支持的应用。
- **SSH** 会为已确认的 SSH 主机打开精简桌面，其中有终端、服务器中心、SSH 文件浏览器、代码编辑器和图片查看器；它不是 RelaxKonOS 工作区。

第一次 SSH 连接时，请在选择「信任并连接」前核对显示的主机密钥指纹。安装与维护流程请参阅[服务器中心](/docs/zh-CN/latest/apps/server-center)。

## 桌面与窗口

- **开始菜单**：点击左下角按钮打开应用列表
- **拖动窗口**：按住标题栏拖动；边缘支持 8 向缩放
- **最小化 / 最大化 / 关闭**：标题栏右侧按钮
- **全屏与连接栏**：全屏时显示 mstsc 风格连接栏，可固定或自动隐藏

## 打开应用

从开始菜单点击任意应用即可打开。多数应用为单窗口（设置、任务管理器、Docker、防火墙等），记事本与代码编辑器支持多窗口。

## 试试看

1. 打开 **终端**，运行 `uname -a` 或 `ver`，观察输出在本地渲染
2. 打开 **文件管理器**，浏览服务器操作系统的目录
3. 打开 **任务管理器**，查看实时性能曲线
4. 断开局域网连接再恢复，终端会话应当仍然存在

> 第 4 步体现了核心差异：会话运行在服务端，客户端只是它的视图。

## 下一步

- [应用程序总览](/docs/zh-CN/latest/apps/terminal)
- [服务器中心](/docs/zh-CN/latest/apps/server-center)
- [协议与通信](/docs/zh-CN/latest/concepts/protocol)

## 客户端与账户入口

- [Android 手机与平板](/docs/zh-CN/latest/getting-started/android)
- [系统账号、Alias 与凭据安全](/docs/zh-CN/latest/getting-started/login)
- [Windows 10/11 个人电脑](/docs/zh-CN/latest/getting-started/windows)
