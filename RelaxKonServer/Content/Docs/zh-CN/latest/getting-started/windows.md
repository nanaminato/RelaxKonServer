---
title: Windows 10/11 个人电脑
description: 在 Windows 10/11 安装个人模式或系统模式，授权本机并配对其他设备。
category: 开始使用
order: 8
---

# Windows 10/11 个人电脑

Windows 10/11 可以在同一台电脑运行 Server 和桌面 Client。本机管理默认选择个人模式，也可选择系统模式。Windows Server 仅支持系统模式。

本页描述当前源码支持；实际可下载的版本见[下载页](/downloads)和[发行说明](/releases/0.1.2)，历史包不一定包含这些能力。

## 选择安装模式

| 模式 | 身份与运行方式 | 安装和维护 |
| --- | --- | --- |
| 个人模式 | 属于发起安装的 Windows 账户；Server 以该账户的非提升权限运行，登录后启动、注销后停止；关闭 Client 不会停止 Server | 本机管理入口，无需 SSH；安装、更新、修复、回退和卸载通过 UAC 配置独立特权助手 |
| 系统模式 | Server、Guardian 和特权助手注册为系统服务，无人登录也能运行；适用于系统服务与多用户主机 | 本机安装需要管理员权限；远程维护需要已提升的管理员 SSH 会话 |

两种安装的程序、数据库、安装标识和操作记录相互独立，不自动转换。共存时必须使用不同端口。Windows 10/11 的系统模式沿用 Windows Server 的权限规则，不把普通域用户自动认定为管理员。

## 安装并授权本机

1. 获取匹配架构的 Windows 桌面客户端与 Server 发布包。打开登录页的**管理本机**，选择个人模式。不需要 OpenSSH Server 或已有 Server 登录。
2. 选择官方发布、本地 ZIP，或提供 SHA-256 的自定义 HTTPS 下载；审阅端口、网络、TLS 和特权文件范围。个人模式使用固定的当前用户目录，服务端包使用 `*-server.zip`。
3. 批准安装阶段的 UAC。即使输入另一管理员的凭据，安装所有者仍是发起安装的用户，数据与登录启动项不会转给那个管理员。取消 UAC 或助手安装失败会使部署失败。
4. 等待操作回执和独立健康检查通过。在同一台电脑通过回环地址连接，选择**设置此 Windows 设备**，使用 Negotiate 验证当前登录账户并注册本机设备公钥，无需输入 Windows 账户密码或 Hello PIN。
5. 个人模式仅允许安装所有者完成本机授权，其他 Windows 管理员不能替代所有者。后续通过设备密钥签名登录；本机密钥遗失时，所有者可从同一本机身份验证入口恢复。

Windows 10/11 系统模式允许本机管理员通过回环 Negotiate 设置或恢复设备密钥；Windows Server 不开放此工作站授权入口。个人模式的所有者授权不适用于另一个系统模式安装。

![截图占位 · 待替换：截图位置：管理本机入口与个人／系统模式选择，展示当前用户与目标模式。](/assets/docs/screenshots/zh-CN/windows-local-mode.svg)

## 权限、数据与日常运行

个人模式的安装所有者默认具有该安装的 RelaxKonOS 管理员能力，无需加入 Windows Administrators。普通文件、终端和 Docker Desktop 使用所有者账户的环境与权限。个人模式不安装 Guardian 系统服务，也不提供跨用户 Guardian；应用能力仍以实际检测结果为准。

程序位于 `%LocalAppData%\RelaxKonOS-Personal\program`，数据位于同级 `data`，操作回执与诊断位于同级 `deployment`。独立的 LocalSystem 助手位于受保护的 `%ProgramFiles%\RelaxKonOS-Personal\<SID>`，不提供网络助手接口。

权限不足的文件操作可由助手执行，但仍受安装时审阅的范围限制：默认仅受管数据目录，可明确选择白名单或全部本地磁盘。显式管理员终端以 LocalSystem 运行，不继承用户的网络盘和域资源凭据；启用它会向所有者及其配对设备授予本机系统级命令执行能力。日常特权操作不逐次请求 UAC，也不保存 Windows 管理员密码。

## 配对其他设备

1. 本机授权成功并以设备密钥登录后，生成配对授权码。
2. 授权码有效期为 10 分钟，只能使用一次；Server 重启会使未使用的码失效。
3. 新的桌面或 Android 客户端生成自己的私钥，使用授权码登记公钥，再通过签名挑战登录。私钥保留在设备，服务端只保存公钥；配对不需要所有者密码或管理员密码。
4. 配对设备使用同一所有者账户和工作区，不创建新 Windows 用户。所有者可查询和撤销设备；撤销会使该设备的访问令牌、刷新令牌及现有 Hub 连接失效，其他设备与密码会话不受影响。

跨设备连接需让 Server 监听 LAN，使用其他设备可达的主机名或 IP，不能把 `127.0.0.1` 或 `localhost` 放入配对地址。配置 HTTPS，证书必须覆盖实际地址。个人模式安装和维护可显式添加防火墙规则，仅允许 Domain/Private 网络的 LocalSubnet 来源；其他范围由 Windows 管理员配置，LAN 不代表公网可达。

![截图占位 · 待替换：截图位置：本机设备授权与配对入口，展示授权结果和设备管理，遮盖有效配对码。](/assets/docs/screenshots/zh-CN/windows-device-pairing.svg)

## 更新、回退与卸载

从本机管理入口维护对应安装，更新、修复、回退和卸载均核对安装标识并按需请求 UAC。刷新状态不请求 UAC。卸载默认保留数据，并移除个人助手与对应防火墙规则；永久删除数据需要单独确认。

当前自动检查覆盖账户隔离、设备配对与撤销、桌面部署请求和系统模式授权回归。普通用户使用另一管理员批准 UAC、LAN HTTPS 与 Android 配对、特权文件及终端、注销重登、回退卸载和双模式共存仍需发布前实机验收。实现与验收范围见[个人电脑维护指南](https://github.com/nanaminato/RelaxKonOS/blob/master/deployment/WindowsPersonalComputer.md)。

## 相关文档

- [安装](/docs/zh-CN/latest/getting-started/installation)
- [登录与账户安全](/docs/zh-CN/latest/getting-started/login)
- [服务器中心](/docs/zh-CN/latest/apps/server-center)

[远程安装：逐步操作](/docs/zh-CN/latest/getting-started/remote-installation) · [更新、卸载与维护](/docs/zh-CN/latest/apps/server-maintenance)
