---
title: 安装
description: 安装 RelaxKonOS 客户端与服务端的准备工作与步骤。
category: 开始使用
order: 2
---

# 安装

RelaxKonOS 由**客户端**与**服务端**两部分组成。客户端安装在你每天使用的设备上，服务端运行在你希望长期保持工作区的机器上。

## 前置要求

- **.NET 10.0 SDK** 或更高版本
- 客户端操作系统：Windows 10/11、macOS、Ubuntu 20.04+
- 服务端操作系统：Ubuntu 20.04+ 或 Windows Server 2016+

## 安装服务端

```bash
cd RelaxKonOS.Server
dotnet run
```

生产环境请务必修改 `appsettings.json` 中的 `Jwt:Secret`（至少 32 位随机字符串），并通过反向代理启用 HTTPS。

## 安装客户端

```bash
cd Client/RelaxKonOS.Client.Desktop
dotnet run
```

客户端启动后会显示登录窗口，输入**宿主系统**的用户名与密码即可登录。

## 首次登录

1. 打开客户端，进入登录窗口
2. 输入服务端所在宿主 OS 的用户凭据
3. 登录成功后进入桌面，Workspace 会自动创建并同步

> 身份由宿主操作系统校验（Windows LogonUser / Linux PAM + NSS），RelaxKonOS 不保存你的密码。

## 下一步

- [快速开始](/docs/zh-CN/latest/getting-started/quick-start)
- [安全模型](/docs/zh-CN/latest/concepts/security)
