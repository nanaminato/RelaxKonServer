---
title: 用户模式安装（Linux）
description: 在普通 Linux 账号下安装 RelaxKonOS 服务端——不需要 sudo，也不改动系统目录。
category: 开始使用
order: 3
---

# 用户模式安装（Linux）

Linux 用户模式适合在自己的普通账号下运行服务端，不需要 sudo，也不安装系统服务或权限助手。默认监听 `127.0.0.1`，远程访问由客户端管理的 SSH 隧道完成。用户模式也支持监听 `0.0.0.0`；在用户配置目录（默认 `~/.config/relaxkonos`，遵循 `XDG_CONFIG_HOME` 或 `RELAXKONOS_USER_CONFIG_ROOT`）的 `listen-host` 文件写入 `0.0.0.0`，再使用用户 launcher 重启服务。客户端安装向导仍默认使用回环地址。

## 通过服务器中心安装

1. 从[下载页](/downloads)取得客户端，并通过服务器中心连接目标 Linux 主机，核对主机密钥。
2. 使用普通非 root 账号执行环境检查，进入安装向导，明确选择 **Linux 用户模式**。
3. 选择官方稳定版、客户端本地 ZIP、服务器上的 ZIP 或自定义 HTTPS 下载。用户模式必须使用 `*-user-server.zip`，系统模式包不能代替。
4. 设置端口（默认 5000），按需填写数据、配置、状态和缓存目录；留空使用下方 XDG 默认目录。
5. 审阅选项后确认安装，等待 `/ready` 检查及最终操作结果记录，随后通过客户端的 SSH 登录隧道登录。

实际发布包以下载目录为准；若用户模式包尚未发布，请先取得适用的发布包。目标主机需具备 Bash、Python 3、curl、unzip、realpath、stat、find、sha256sum 和 flock，不需要 systemd。

## 目录与权限

| 用途 | 默认路径 |
| --- | --- |
| 程序与版本 | `${XDG_DATA_HOME:-$HOME/.local/share}/relaxkonos/` |
| 配置与密钥 | `${XDG_CONFIG_HOME:-$HOME/.config}/relaxkonos/` |
| 状态、数据库与日志 | `${XDG_STATE_HOME:-$HOME/.local/state}/relaxkonos/` |
| 缓存 | `${XDG_CACHE_HOME:-$HOME/.cache}/relaxkonos/` |

目录必须是绝对路径，避免相互重叠。私有目录与文件采用 `0700` / `0600`。客户端会保存服务器侧目录定位，重连后的维护继续使用实际安装位置。

用户模式不安装 PAM 或 sudoers 配置，不更改系统防火墙，不提供系统模式的 Docker 等特权功能。网络默认仅本机，可在用户配置中启用 LAN 监听，不配置系统模式的 TLS 和特权文件范围。

## 更新与卸载

在服务器中心选中主机，通过维护操作升级、修复、回滚或卸载；无需额外 SSH 客户端或手动运行生命周期命令。升级就绪失败时执行恢复，结果以操作结果记录为准。卸载默认保留数据，永久删除数据需要单独确认。

端口占用、依赖缺失、包类型或架构不匹配、校验失败和操作锁冲突都会阻断操作；请查看环境检查与操作记录，修复原因后重试。

完整来源、目录与维护选项见[安装指南](/docs/zh-CN/latest/getting-started/installation)，入口说明见[服务器中心](/docs/zh-CN/latest/apps/server-center)。
