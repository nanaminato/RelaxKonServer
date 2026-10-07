---
title: 隧道管理器
description: 管理 FRP 内网穿透的期望状态与运行时，让 frpc / frps 作为独立进程运行。
category: 应用程序
order: 40
---

# 隧道管理器

隧道管理器（Tunnel Manager）通过独立的隧道进程，将服务器上的服务提供给远端访问。

核心取舍是：**RelaxKonOS 原生管理隧道，但 `frpc` / `frps` 始终作为独立进程运行**。实际流量只在 FRP 进程之间转发，RelaxKonOS 既不参与数据转发，也不依赖隧道存活来维持自身可用。

## 概览

- **数据库是事实来源，生成的配置不是。** 隧道与服务器配置以可校验、可审计的期望状态保存，生成的配置文件只是产物。
- **面向 Provider 建模**，FRP 只是第一个实现；未来增加其他隧道方案时不需要改动调用方。
- RelaxKonOS 的 Server、认证、局域网访问与修复路径**不依赖隧道存活**。

## 适用场景

- 需要在没有公网入口的机器上暴露一个或多个端口。
- 需要连接 RelaxKonOS 管理的服务端、自建服务端或第三方兼容服务端。
- 既要用 RelaxKonOS 管理的运行时，也想使用操作系统中已有的 FRP。

## 当前能力

- 隧道定义：以期望状态保存，应用前校验。
- 服务器档案：主机、端口、认证与传输等非秘密部分；秘密材料只在服务端内部引用。
- 运行时管理：下载、校验、版本化安装、启动、停止、状态、日志、升级与回滚；升级是版本目录指针切换，而不是覆盖既有二进制。
- 外部运行时：只检测运行时，并在用户明确授权后由 RelaxKonOS 启动，**绝不接管未经授权的配置或进程**。
- 隧道类型：首个版本支持 `tcp`、`udp`、`http`、`https`。
- 管理端服务端：可管理由 RelaxKonOS 托管的服务端，并提供配置与诊断页面。
- 界面：概览、隧道、服务器、运行时与日志、安全状态。

## 权限与安全

- 秘密信息不通过普通接口、日志、生成配置的下载接口或客户端回显泄露。
- 运行时可执行文件的路径与命令行由服务端常量与结构化参数决定，**不拼接 shell 字符串**。
- 如果操作系统拦截或隔离了运行时二进制，RelaxKonOS 只显示诊断信息；**不会**关闭防病毒软件、添加全局排除项、加壳或篡改二进制来绕过安全软件。
- 变更类操作需要相应的读取与管理权限，并记录审计。

## 平台差异

Windows、Windows Server 与 Linux 均受支持。差异主要在于运行时的进程监管方式与操作系统的安全软件行为，均封装在服务端。

## 已知限制

- 当前版本**不包含** `STCP`、`XTCP`、P2P visitor、插件、自定义配置片段、任意环境变量或任意命令参数透传；这些需要独立的秘密、网络暴露与输入模型。
- **不会自动把 RelaxKonOS 自身暴露到公网。** 该能力属于单独的远程访问 / TLS / 身份审查范围，应以显式、可撤销的系统隧道实现。
- **不会自动安装或强制启用服务端二进制**；服务端属于后续可选项，不阻塞客户端主路径。
- Cloudflare Tunnel、Tailscale 等只保留抽象边界，V1 不实现。
- 不嵌入或改写隧道协议，也不让 RelaxKonOS 参与数据转发。

## 相关文档

- [端口转发](/docs/zh-CN/latest/apps/port-forwarding)
- [代理管理器](/docs/zh-CN/latest/apps/proxy-manager)
- [安全模型](/docs/zh-CN/latest/concepts/security)

## 独立组件服务与维护边界

Windows 的受管 Nginx、Mihomo、FRPC/FRPS 使用独立 SCM 服务；Linux 的受管 FRPC/FRPS 使用独立 systemd 服务，每个 FRPC 配置对应独立实例。停止或卸载 Server、Guardian、Helper 不会自动停止保留的组件。重启 Server 后从持久化记录恢复管理状态，而不是依赖内存中的 PID。外部运行时仍须明确授权，不会自动转为受管服务。

默认卸载保留组件运行、配置及所有权记录；按原数据位置重装后可重新管理。完整删除数据会先清理受管 SMB、Nginx、FRP、Mihomo；清理失败或所有权冲突时保留程序、Helper 和数据，先读取失败回执再处理。接入的系统 Nginx 及其他站点、共享文件受所有权边界保护。卸载界面现可分别选择移除 SMB、Nginx、FRP、Mihomo，默认全部保留。保留任一组件时保留整个数据根与管理记录；勾选完整删除数据会选择移除全部四项，取消任一组件的移除会取消数据删除。Docker Engine、容器及卷不属于这四项清理范围。

隔离 Windows/Linux 主机的服务生命周期、系统重启、保留数据卸载与重装已验证；GUI/API 全流程和跨版本升级仍需回归。详见[独立组件实现与验收](https://github.com/nanaminato/RelaxKonOS/blob/master/docs/services/RelaxKonOS.IndependentComponentServices.Progress.md)与[更新、卸载与维护](/docs/zh-CN/latest/apps/server-maintenance)。
