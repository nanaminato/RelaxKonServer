---
title: 设置
description: 集中管理桌面外观、语言与工作区偏好，并同步到多台设备。
category: 应用程序
order: 24
---

# 设置

设置中心（Settings）采用 Windows 11 / GNOME 风格，是工作区偏好的统一入口。

## 分类页

| 分类 | 内容 |
| --- | --- |
| 系统 | 系统信息、关于与更新状态 |
| 个性化 | 壁纸、主题调色板、桌面图标 |
| 时间和语言 | 时间 / 日期格式、语言与区域 |
| 网络 | 网卡与时区（只读，来自宿主 OS） |
| 应用 | 默认程序、应用权限 |
| 镜像源 | APT / Docker / NPM 等镜像源 |
| 开发者 | 开发模式与应用包 |

## 偏好持久化

用户偏好通过 `/api/v1.0/workspaces/{id}/preferences` 保存到 **Workspace**，因此会在多台设备之间共享。登录时 `PreferencesSync` 自动加载偏好，壁纸、任务栏底色与时钟格式即时生效；编辑后防抖 300ms 保存。

## 权限边界

宿主 OS 级设置（时区、网卡）只做**只读展示**。RelaxKonOS 不修改宿主 OS 的系统配置，这属于硬约束「权限提升委托宿主 OS」。

## 相关文档

- [工作区](/docs/zh-CN/latest/concepts/workspace)
- [持久化](/docs/zh-CN/latest/concepts/persistence)
