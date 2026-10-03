---
title: 登录与账户安全
description: 登录与账户安全
category: 开始使用
order: 6
---

# 登录与账户安全

> 本页对照当前源码说明能力，不是某个发布包的功能清单。已发布产物与日期见[发行说明](/releases/0.1.2)，功能验收状态以所链接的实现记录为准。

## 系统登录与 Alias

系统登录由 Windows LogonUser 或 Linux PAM 验证。系统模式用户可先用真实系统账号登录，在设置中创建独立的 Alias 与密码；两种凭据绑定同一服务器身份、User 和 Workspace，Alias 不创建新 OS 用户或额外文件权限。

Alias 密码只保存单向哈希。服务端不持久保存系统登录密码；用户显式选择记住凭据时，桌面使用 OS 安全存储，Android 使用本机保险箱。这些本机凭据不属于 Workspace 同步数据。

## 管理登录方式

在账户设置中可以查询、创建、更名、改密或删除 Alias，敏感变更需要当前凭据复验。关闭系统账号直接登录前必须已有有效 Alias；这只改变 RelaxKonOS 登录策略，不修改 SSH、SMB 或系统账号。删除 Alias 需要系统密码复验并恢复系统登录。密码与安全策略变更可能撤销旧会话，需要重新登录。

## 模式与授权

用户模式只接受运行 Server 的当前 Unix 账号，不支持 Alias 或管理员授权。系统模式中的 Alias 登录不会继承系统密码认证的免重复管理员授权；普通用户和 Alias 会话必须明确认证所选管理员。跨账号守护与脚本仍需本次明确批准，文件操作按独立的路径授权执行。

参考：[认证实现](https://github.com/nanaminato/RelaxKonOS/blob/master/RelaxKonOS.Server/Identity/LoginAuthenticationService.cs)、[Alias 管理](https://github.com/nanaminato/RelaxKonOS/blob/master/RelaxKonOS.Server/Identity/AliasCredentialService.cs)、[特权操作](https://github.com/nanaminato/RelaxKonOS/blob/master/docs/platform/RelaxKonOS.PrivilegedOperations.Operations.md)。

[用户模式](/docs/zh-CN/latest/getting-started/user-mode) · [安全模型](/docs/zh-CN/latest/concepts/security)
