---
title: Android 手机与平板
description: Android 手机与平板
category: 开始使用
order: 5
---

# Android 手机与平板

> 本页对照当前源码说明能力，不是某个发布包的功能清单。已发布产物与日期见[发行说明](/releases/0.1.2)，功能验收状态以所链接的实现记录为准。

Android 客户端是独立的 Kotlin、Jetpack Compose 与 Material 3 应用，面向手机和平板，不使用 Avalonia 或 .NET Android。最低支持 Android 10（API 29）；这不代表全部功能已在所有系统版本和设备上验收。

## 连接与功能

- RelaxKonOS Server 连接提供已登录账号的工作区、文件、终端、监控和能力门控的服务管理。
- SSH 连接提供主机密钥确认、SSH 终端、SFTP 和服务器维护；它不创建完整 RelaxKonOS 工作区。
- 手机和平板使用自适应导航。Shell 支持紧凑、中等和展开布局，不代表每个页面都已完成平板分栏验收。
- 已接入文件分块上传/续传、Docker/Compose、Nginx/站点、证书、Mihomo、FRP、SMB、UFW、Git、应用部署、任务管理和守护页面。具体动作仍受平台、服务器模式及授权约束。
- 原生移动应用模型独立于桌面 .roapp；桌面扩展包不能直接在 Android 上运行。

## 安装与第一次连接

1. 在[下载页](/downloads)查找已发布的 Android 签名 APK，核对 SHA-256；没有下载项就表示本站尚未提供该包。
2. 在系统安装界面确认安装；更新必须保持同一签名证书。AAB 用于商店发布，不能作为直接安装包。
3. 填入设备实际可访问的服务端地址，或添加 SSH 主机并核对指纹。真机不能把开发电脑的 localhost 当作远程地址。
4. 选择系统账号或可用的 Alias 登录。连接用户模式服务端时遵守 SSH 转发与 loopback 边界。

## 实现与验收

首次安装与维护执行链已接入，完整设备/真实服务器环境验收仍独立追踪。可靠后台告警、卷/数据库恢复和 Android 恢复提交界面不能作为已交付功能宣传。详细移动规范与进度由 Android 工程维护，官网只提供此入门摘要。

权威来源：[Android 文档](https://github.com/nanaminato/RelaxKonOS/blob/master/Client/RelaxKonOS.Client.Android/docs/README.md)、[当前实现](https://github.com/nanaminato/RelaxKonOS/blob/master/Client/RelaxKonOS.Client.Android/docs/status/Progress.md)、[测试与验收](https://github.com/nanaminato/RelaxKonOS/blob/master/Client/RelaxKonOS.Client.Android/docs/status/Verification.md)、[签名与发布](https://github.com/nanaminato/RelaxKonOS/blob/master/Client/RelaxKonOS.Client.Android/docs/development/android-release.md)。
