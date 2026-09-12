---
title: 文件管理器
description: 基于服务端 REST API 与宿主 OS 权限的远端文件管理。
category: 应用程序
order: 12
---

# 文件管理器

文件管理器（Explorer）让你像操作本地磁盘一样操作**服务端宿主系统**的文件，但它并不是远程桌面式的文件浏览。

## 概述

界面移植自 Jaya File Manager，所有文件操作都通过服务端 REST API（`/api/v1.0/files/*`）执行。服务端以宿主 OS 进程身份调用文件系统，因此**沿用宿主系统的用户与权限**，不额外建立一套 ACL。

## 功能

- 导航树（懒加载）+ 网格视图 + 地址栏 + 工具栏 + 状态栏
- 浏览、新建文件夹、删除、重命名、复制、移动
- 上传与下载
- 文件 / 目录属性查看，Linux 下可编辑 POSIX 权限
- 依据应用 manifest 的扩展名声明进行「默认打开」或「打开方式」

## 使用方式

从开始菜单打开文件管理器，双击目录进入，双击文件按声明方式打开。删除等危险操作会弹出确认对话框。

## 权限与安全

- 所有操作以**登录用户**在宿主 OS 上的身份执行
- 越权访问会被宿主 OS 直接拒绝，RelaxKonOS 不会绕过它
- 没有独立的权限提升通道，提权一律委托宿主 OS

## 架构

```text
Explorer UI (Client)
      |
  REST /api/v1.0/files/*
      |
RelaxKonOS.Server
      |
  System.IO 以宿主用户身份
```

## 相关文档

- [文件服务](/docs/zh-CN/latest/apps/file-services)
- [安全模型](/docs/zh-CN/latest/concepts/security)
