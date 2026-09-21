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
| 网络 | 网卡与时区（只读，来自宿主 OS）、统一出站代理 |
| 应用 | 默认程序、应用权限 |
| 镜像源 | APT / NPM 等镜像源；**Docker 镜像源已移至 Docker 管理器** |
| 开发者 | 开发模式、应用包与[网络诊断器](/docs/zh-CN/latest/apps/network-inspector)入口 |

## 出站代理

「网络」分类中的出站代理是**服务器上的一份共享配置**，所有用户看到同一份设置。它只影响明确勾选的内置功能，**不会更改宿主操作系统的系统代理**。

- **代理来源**：手动填写的地址，或复用内置代理运行时的监听端口。选择内置来源时，界面会说明当前是否真的在监听。
- **HTTPS 地址**：留空时复用 HTTP 代理地址。
- **不使用代理的地址**：多项之间用 `;` 或 `,` 分隔。
- **作用范围**：按功能分别勾选，例如 Docker 守护进程拉取镜像、Docker 构建过程、镜像标签与版本查询、内置运行时下载。
- 启用或移除 Docker 守护进程层代理会重启 Docker 并中断运行中的容器，因此会先要求确认。

同一份配置也会反映在 [Docker 管理器](/docs/zh-CN/latest/apps/docker)的「网络代理」页面；两处编辑的是同一个设置，不存在第二份真源。

## 偏好持久化

工作区偏好通过 `/api/v1.0/workspaces/{id}/preferences` 保存到 **Workspace**，因此会在多台设备之间共享。登录时 `PreferencesSync` 自动加载偏好，壁纸、任务栏底色与时钟格式即时生效；编辑后防抖 300ms 保存。

出站代理是这一规则的例外：它属于**服务器级**配置，由所有用户共享，不随工作区同步。

## 权限边界

宿主 OS 级设置（时区、网卡）只做**只读展示**。RelaxKonOS 不修改宿主 OS 的系统配置，这属于硬约束「权限提升委托宿主 OS」。

## 相关文档

- [Docker 管理器](/docs/zh-CN/latest/apps/docker)
- [代理管理器](/docs/zh-CN/latest/apps/proxy-manager)
- [网络诊断器](/docs/zh-CN/latest/apps/network-inspector)
- [工作区](/docs/zh-CN/latest/concepts/workspace)
