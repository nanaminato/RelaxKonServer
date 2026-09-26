---
title: 代码编辑器
description: 在远端编辑代码文件，支持语法高亮与多编码。
category: 应用程序
order: 18
---

# 代码编辑器

代码编辑器用于编辑 RelaxKonOS Server 宿主机上的代码文件，支持语法高亮与多编码打开与保存。在已确认主机密钥的 SSH 桌面中，它也可以通过 SFTP 打开、浏览和保存代码。

## 功能

- 语法高亮与基础编辑体验
- 多编码读取与写回（UTF-8 / GBK / Shift-JIS 等）
- 与记事本共享编码选择对话框
- 支持多窗口打开多个文件

## 使用方式

从开始菜单打开代码编辑器，或从文件管理器中按扩展名声明用「打开方式」启动。在 SSH 桌面中，请从 SSH 文件浏览器打开代码文件。保存会写回已连接的服务端或 SSH 主机，默认不会把远程文件写到客户端。

## 权限与安全

- 文件的读写以登录用户在宿主 OS 上的权限执行
- 超出权限范围的路径会被宿主 OS 拒绝
- 编辑器不引入独立的文件访问通道

## 相关文档

- [记事本](/docs/zh-CN/latest/apps/notepad)
- [文件管理器](/docs/zh-CN/latest/apps/file-manager)
- [服务器中心](/docs/zh-CN/latest/apps/server-center)
