---
title: 安全模型
description: RelaxKonOS 的身份、权限与提权边界。
category: 概念
order: 64
---

# 安全模型

> 本页说明当前源码能力；发布包内容见[发行说明](/releases/0.1.2)，验收状态见相关实现文档。

系统身份和执行权限仍来自宿主 OS，但系统登录与 Alias 是不同的凭据入口。Windows 系统登录使用 LogonUser，Linux 使用 PAM/NSS；Alias 使用独立单向密码哈希，并绑定原有宿主用户和 Workspace。服务端不持久保存系统登录密码，用户主动记住的本机凭据另由 OS 安全存储或 Android 保险箱管理。

## 文件与应用授权

Windows 10/11 个人模式绑定发起安装用户的 SID；所有者具有本安装的 RelaxKonOS 管理员能力，但 Server 仍以非提升令牌运行。安装与维护通过 UAC 设置独立 LocalSystem 助手，使用另一管理员批准 UAC 不改变所有者。文件提权受安装审阅的范围限制；显式管理员终端则授予所有者及配对设备 LocalSystem 命令执行能力。

本机授权仅允许所有者，设备私钥留在设备，撤销设备会失效其令牌并断开 Hub。个人模式不提供跨用户 Guardian，Windows Server 不支持个人模式。详见[Windows 10/11](/docs/zh-CN/latest/getting-started/windows)。

普通文件操作受绑定宿主身份、路径与 OS 权限约束；应用权限不能代替 OS 权限。受保护目录使用独立的文件授权与窄 Helper 契约。拒绝读取会明确显示无权访问，而不是空目录。Linux 用户模式只访问当前 Unix home，不提供文件提权。

## 管理员授权与 Helper

系统模式中的系统认证管理员/root 会话动态核验资格，符合条件的非文件操作免重复密码。普通用户和 Alias 会话通过明确认证所选有效管理员取得精确、短期、令牌绑定的授权；跨账号 Guardian/脚本仍需本次显式批准。文件操作使用独立的路径授权规则。

Server 保持最小权限；Helper 只执行固定、强类型且经过审计的动作。安装权限、服务账户能否调用 Helper 和当前用户能否管理目标是不同条件，不能互相替代。Docker socket 权限必须在系统安装时显式选择，Linux 用户模式禁用 Docker、防火墙、代理、证书等宿主管理能力。Helper 缺失或拒绝时功能报错，不改用高权限 Server 绕过。

## 凭据与诊断

密码、私钥和令牌不得进入日志、审计或诊断。Alias 哈希不同于可逆秘密；需要长期保存的应用秘密使用受控加密存储。本机保险箱与 SSH 凭据不随 Workspace 同步。更换或删除凭据可能撤销已有会话。

HTTPS、输入校验、路径规范化、授权和审计共同约束操作。上传、危险动作与恢复仍各有确认边界；个人模式的显式管理员终端具有上述 LocalSystem 执行权限，不应按普通终端的权限理解。

[登录与账户安全](/docs/zh-CN/latest/getting-started/login) · [文件上传与续传](/docs/zh-CN/latest/apps/file-transfers)

[运维授权说明](https://github.com/nanaminato/RelaxKonOS/blob/master/docs/platform/RelaxKonOS.PrivilegedOperations.Operations.md)
