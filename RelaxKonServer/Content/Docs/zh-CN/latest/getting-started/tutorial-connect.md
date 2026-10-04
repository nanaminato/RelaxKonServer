---
title: 第一次连接服务器
description: 从选择连接方式到进入 RelaxKonOS 桌面。
category: 开始使用
order: 11
---

# 第一次连接服务器

以下步骤以桌面客户端和 RelaxKonOS Server 工作区为例。功能是否可用取决于客户端版本、服务端版本、安装模式和账户权限。

## 1. 准备客户端和服务器

从[下载页](/downloads)取得适合设备的客户端。准备服务器地址和账户；还没有安装服务端时，先阅读[安装指南](/docs/zh-CN/latest/getting-started/installation)或[Linux 用户模式安装](/docs/zh-CN/latest/getting-started/user-mode)。

![截图位置：准备客户端和服务器](/assets/tutorials/zh-CN/tutorial-connect-1.svg)

*截图位置：准备客户端和服务器*

## 2. 选择连接方式

打开客户端，选择 RelaxKonOS Server 连接以进入完整工作区。独立 SSH 连接提供精简桌面，适合终端、SFTP 与服务器维护；请根据自己的目标选择。

![截图位置：选择连接方式](/assets/tutorials/zh-CN/tutorial-connect-2.svg)

*截图位置：选择连接方式*

## 3. 填写连接信息

填写服务端地址与登录凭据。Linux 用户模式默认使用回环地址，可在 Server 登录页选择“通过 SSH 隧道连接”，填写 SSH 主机、端口、账号，以及服务器上的地址（例如 http://127.0.0.1:5000）。首次 SSH 连接需核对主机密钥指纹。

![截图位置：填写连接信息](/assets/tutorials/zh-CN/tutorial-connect-3.svg)

*截图位置：填写连接信息*

## 4. 登录并确认桌面

连接成功后确认桌面和开始菜单已显示。SSH 凭据与 Server 凭据是两组信息，只有明确选择复用且采用密码认证时才共用。登录失败时检查地址、端口、账户和服务端状态，详细说明见[登录与账户安全](/docs/zh-CN/latest/getting-started/login)。

![截图位置：登录并确认桌面](/assets/tutorials/zh-CN/tutorial-connect-4.svg)

*截图位置：登录并确认桌面*

[下一篇教程：熟悉桌面与窗口](/docs/zh-CN/latest/getting-started/tutorial-desktop)
