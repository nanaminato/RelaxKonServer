---
title: 服务器中心
description: 通过 SSH 连接主机，并安装、检查或维护 RelaxKonOS Server；SSH 不会被当作完整工作区。
category: 应用程序
order: 51
---

# 服务器中心

> 本页说明当前源码能力；发布包内容见[发行说明](/releases/0.1.2)，验收状态见相关实现文档。

服务器中心用于连接和部署远程主机，支持 RelaxKonOS Server 和直接 SSH 两种连接方式。

## Windows 10/11 个人电脑

Windows 10/11 桌面客户端还提供登录页的**管理本机**入口，无需 SSH 或已有 Server 登录。默认个人模式，也可选系统模式。安装、更新、修复、回退和卸载按所选安装身份执行，并在配置特权助手时请求 UAC；刷新状态不请求 UAC。个人模式归属于发起安装的用户，即使用另一管理员批准 UAC 也不改变归属。流程与本机授权见[Windows 10/11 个人电脑](/docs/zh-CN/latest/getting-started/windows)。

## 选择连接方式

| 连接方式 | 可获得的能力 | 不提供的能力 |
| --- | --- | --- |
| RelaxKonOS Server | 登录、持久工作区、偏好同步，以及该账号可用的服务端能力 | 不能替代对任意主机进行 SSH 管理 |
| SSH | 已确认 SSH 主机的精简桌面：终端、服务器中心、SSH 文件浏览器、代码编辑器与图片查看器 | 不创建 RelaxKonOS 工作区，也不能使用 Docker、防火墙、证书管理器等仅服务端应用 |

SSH 适合在服务器尚未安装前连接主机，或用于维护已有主机；它不会把 SSH 端点变成已接入管理的 RelaxKonOS Server。

## SSH 信任与已保存连接

首次连接 SSH 主机前，客户端会显示主机密钥指纹。只有明确选择「信任并连接」才会保存；取消或关闭对话框不会更改任何信任记录。已保存的连接记录主机、端口和用户。若保存密码，则由操作系统的安全凭据库处理，不会保存到 RelaxKonOS 工作区中。

SSH 桌面始终跟随客户端系统语言，不读取或覆盖 RelaxKonOS 工作区的语言偏好。

## 安装与维护服务端

对于已确认的 SSH 主机，服务器中心可以执行预检、读取主机状态，并引导完成：

- 使用已验证发布包进行首次安装或升级
- 修复、回滚与卸载
- 读取主机侧最终操作结果记录，而不是把启动器退出码直接当作成功

Linux 系统模式可以使用 root 或经验证的 sudo；Linux 用户模式使用普通账号且不需要 sudo。Windows 系统模式要求 SSH 会话已提升管理员权限。安装向导覆盖来源、端口、目录、网络、证书、文件访问范围、Linux 分身份授权与 Docker 选项，详见[安装指南](/docs/zh-CN/latest/getting-started/installation)。

卸载默认保留服务端数据。删除数据属于显式的破坏性操作，必须再次确认服务器名称。

## 安全边界

- 只有显示预检结果并经用户明确确认后才会执行部署。
- SSH 主机密钥验证避免在主机变化时静默接受连接。
- 服务器中心不会把 SSH 连接变成提权通道；改变服务器状态的功能仍受服务端既有授权模型约束。

## 相关文档

- [安装](/docs/zh-CN/latest/getting-started/installation)
- [用户模式安装](/docs/zh-CN/latest/getting-started/user-mode)
- [终端](/docs/zh-CN/latest/apps/terminal)
- [文件管理器](/docs/zh-CN/latest/apps/file-manager)
- [安全模型](/docs/zh-CN/latest/concepts/security)

[Android 服务器中心与独立验收状态](/docs/zh-CN/latest/getting-started/android)

[远程安装：逐步操作](/docs/zh-CN/latest/getting-started/remote-installation) · [更新、卸载与维护](/docs/zh-CN/latest/apps/server-maintenance)
