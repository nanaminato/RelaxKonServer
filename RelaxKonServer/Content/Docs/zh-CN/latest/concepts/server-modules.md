---
title: 服务端模块
description: 服务端功能以模块为单位启用、驻留与按需加载，以及它与组件、应用的区别。
category: 概念
order: 68
---

# 服务端模块

RelaxKonOS 的服务端不是一整块程序。Core 负责启动、身份认证、工作区、基础存储、协议与恢复；其余服务端功能以**模块**为单位注册，可以按需启用，也可以在空闲时把内存释放掉。客户端只看到「这台服务器当前能做什么」，不关心模块怎么装载。

## 模块、组件与应用

| 对象 | 是什么 | 归属 | 启用模块会顺带安装它吗 |
| --- | --- | --- | --- |
| 服务端模块 | RelaxKonOS 自己的服务端功能单元，例如终端、Git、Docker 管理 | 组件中心 → 服务端模块 | — |
| 组件 | Nginx、Mihomo、Docker Engine、FRP 等宿主软件 | [组件中心](/docs/zh-CN/latest/apps/component-center) | 不会 |
| 应用 | 客户端上的窗口与界面，例如终端、文件管理器、Docker 页面 | 各应用自身 | 不会 |

这条边界是单向的：启用 Docker 模块不会安装 Docker Engine；停用或休眠模块也不会停止已安装的 Docker、删除容器、清除业务配置或用户数据。软件本身的安装、更新与卸载始终走组件的受管流程。模块已启用但软件缺失时，界面应能发现入口并把你带到组件中心，而不是把「模块已启用」当成「功能可用」。

## 官方模块

当前编译进 Host 的官方模块有 9 个。配置名由模块 ID 推导，`relaxkonos.system-performance` 对应 `Modules:SystemPerformance`：

| 模块 | 模块 ID | 配置名 |
| --- | --- | --- |
| 终端 | `relaxkonos.terminal` | `Terminal` |
| 防火墙 | `relaxkonos.firewall` | `Firewall` |
| 系统性能 | `relaxkonos.system-performance` | `SystemPerformance` |
| Git | `relaxkonos.git` | `Git` |
| Tailscale | `relaxkonos.tailscale` | `Tailscale` |
| Docker | `relaxkonos.docker` | `Docker` |
| 证书 | `relaxkonos.certificates` | `Certificates` |
| Web 服务器 | `relaxkonos.webserver` | `Webserver` |
| 代理 | `relaxkonos.proxy` | `Proxy` |

Core 固定保留：启动、认证、宿主模式、授权、基础存储、工作区、协议、Helper 通道与恢复入口都不属于可停用模块。

## 启用与生效

- 开关是配置项 `Modules:<配置名>:Enabled`，例如 `Modules:Git:Enabled=false`。**重启后生效**，本进程的模块集合在启动时冻结。
- 不写任何 `Modules` 配置时默认全部启用，与引入模块化之前的行为一致；未知模块名、非布尔值或未知设置会让启动直接失败，而不是悄悄改变集合。
- 登录后可读只读清单 `GET /api/v1.0/server/modules`，返回每个模块的有效启用状态、依赖、阻止停用的原因与驻留状态。**查询状态不会唤醒任何模块**。
- 停用前会检查依赖与反向依赖：仍有启用模块依赖它、或存在未完成任务与待恢复职责时给出阻止原因，而不是静默级联停用。

## 驻留与按需加载

模块的「卸载」只指**从内存释放**，不删文件、不改配置、不动业务数据，也不停止外部服务。

| 策略 | 行为 |
| --- | --- |
| 保持加载 | 首次加载后常驻 |
| 闲时延迟释放（默认） | 最后一个租约释放后再等宽限期，默认 300 秒 |
| 退出后立即尝试释放 | 会话成功退出后立即检查该会话用过的模块；仍被他人使用则等最后一个租约结束 |

- 唤醒发生在业务请求到来时：认证、授权、有效启用与平台检查**先通过**，再取得模块租约。未认证或无权限的请求不会触发加载，普通请求也不会下载模块、启用模块或安装软件。
- 同一模块的并发首次访问会合并成一次加载。唤醒有 30 秒时限与排队上限，超时或过载返回稳定的暂不可用错误，不重放可能产生副作用的请求。
- 目前声明可释放对象图的是 Git、防火墙与 Tailscale，释放级别为「对象 / 缓存 / 连接」。程序集卸载与独立进程退出尚未交付，因此不会承诺工作集立即下降。
- 加载失败会保留稳定原因：`module-load-failed`、`module-wake-timeout`、`module-wake-overloaded`、`module-host-stopping`。界面显示原因与重试入口，不把失败显示成成功。

## 在哪里管理

- **桌面客户端**：组件中心 → 服务端模块，查看清单、依赖、驻留状态与健康检查。启用、停用、唤醒、尝试休眠与调整驻留策略都先出预览计划再提交，模块包的上传安装也在桌面端。
- **Android**：同一入口下可查看清单并提交相同的计划类操作，不含模块包上传。
- **设置 → 系统**只保留只读摘要与「在组件中心管理」的跳转。

## 当前状态与发布口径

- 模块目录与启停开关在 2026-10-28 进入源码。已发布的 0.2.0-v1a（2026-10-04）**早于**这部分工作，升级到包含模块化的构建后才会看到本文描述的清单与驻留状态。
- 第三方模块使用独立包格式 `.rkmodule`，配合来源信任、提权确认与独立模块宿主（ModuleHost）；Windows 与 Linux 已有平台验收记录，具体契约与边界以产品仓库 `docs/architecture/RelaxKonOS.ServerModule*.md` 为准。
- 本文描述的是当前源码能力。某个已发布包是否包含某项能力，以该版本的发行记录为准，不能由源码支持反推。

## 相关文档

- [客户端 / 服务端架构](/docs/zh-CN/latest/concepts/architecture)
- [应用模型](/docs/zh-CN/latest/concepts/application-model)
- [组件中心](/docs/zh-CN/latest/apps/component-center)
- [终端](/docs/zh-CN/latest/apps/terminal)
