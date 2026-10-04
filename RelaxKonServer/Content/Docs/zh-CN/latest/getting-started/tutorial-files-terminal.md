---
title: 用文件管理器和终端完成第一次操作
description: 浏览服务器文件，并用只读命令确认终端所在的主机。
category: 开始使用
order: 17
---

# 用文件管理器和终端完成第一次操作

以下步骤以桌面客户端和 RelaxKonOS Server 工作区为例。功能是否可用取决于客户端版本、服务端版本、安装模式和账户权限。

## 1. 浏览服务器目录

打开文件管理器，进入自己的用户目录。双击文件夹进入，使用地址栏确认当前位置。用户模式限制在当前 Unix home；遇到无权访问时请检查账户和路径。

![截图位置：浏览服务器目录](/assets/tutorials/zh-CN/tutorial-files-terminal-1.svg)

*截图位置：浏览服务器目录*

## 2. 创建练习文件夹

在有写入权限的目录中新建文件夹，例如 relaxkon-tutorial，再试着重命名它。仅使用自己的练习文件；删除时仔细核对确认窗口中的目标。上传与下载操作见[文件传输](/docs/zh-CN/latest/apps/file-transfers)。

![截图位置：创建练习文件夹](/assets/tutorials/zh-CN/tutorial-files-terminal-2.svg)

*截图位置：创建练习文件夹*

## 3. 确认终端运行位置

从开始菜单打开终端。在 Linux shell 中依次运行 pwd、whoami、uname -a；Windows 命令提示符可运行 cd、whoami、ver。这些只读命令帮助确认服务器身份、系统和当前目录。

![截图位置：确认终端运行位置](/assets/tutorials/zh-CN/tutorial-files-terminal-3.svg)

*截图位置：确认终端运行位置*

## 4. 理解文件与会话

文件管理器和终端操作的都是服务器资源，终端工作目录不会自动跟随文件管理器。短暂断线时 Server 终端可保留会话；服务端重启、主动结束会话等情况应另行检查，不能据此假定任务永久运行。详见[终端](/docs/zh-CN/latest/apps/terminal)。

![截图位置：理解文件与会话](/assets/tutorials/zh-CN/tutorial-files-terminal-4.svg)

*截图位置：理解文件与会话*
