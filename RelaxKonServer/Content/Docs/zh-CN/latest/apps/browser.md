---
title: 浏览器
description: 使用客户端原生引擎渲染网页，并把书签与历史持久化到工作区。
category: 应用程序
order: 16
---

# 浏览器

内置浏览器（RemoteBrowser）**不是远程浏览器**。网页内容通过客户端自身的网络与原生引擎渲染。

## 概述

浏览器基于 `Avalonia.Controls.WebView` 的 `NativeWebView`，在 Windows 上使用 WebView2，macOS 使用 WKWebView，Linux 使用 WebKitGTK。网页流量不经过服务端。

## 功能

- 导航：后退、前进、刷新、停止、主页、地址栏
- 书签：新增、删除、侧边栏双击导航、清空
- 历史：自动记录、侧边栏双击导航、单条删除、清空
- 浏览器偏好同步到 Workspace（主页、链接打开位置）

## 使用方式

打开浏览器后像平时一样访问网站。若需要访问服务端侧的 loopback 地址，可先用**端口转发**把远端端口映射到本机 localhost。

## 同步到服务端的内容

| 内容 | 是否同步 |
| --- | --- |
| 书签、历史记录 | 是（按用户隔离） |
| 主页、链接打开位置 | 是 |
| Cookie、扩展配置 | 否 |
| 网页内容 | 否，走客户端网络 |

## 安全

- 书签与历史通过 `/api/v1.0/browser/*` 持久化，按用户隔离
- 本地端口转发只监听 `127.0.0.1`
- 未登录时会提示先登录

## 相关文档

- [端口转发](/docs/zh-CN/latest/apps/port-forwarding)
- [工作区](/docs/zh-CN/latest/concepts/workspace)
