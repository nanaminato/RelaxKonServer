---
title: 用户模式安装（Linux）
description: 在普通 Linux 账号下安装 RelaxKonOS 服务端——不需要 sudo，也不改动系统目录。
category: 开始使用
order: 3
---

# 用户模式安装（Linux）

**用户模式（User Mode）** 面向「我只想在自己的 Linux 账号下跑一份服务端」的场景。它与系统模式共用同一套 Server 与 Guardian 二进制，但把全部持久状态限制在该账号的 XDG 目录中：不创建 systemd 系统服务、不安装常驻权限助手、不修改 PAM、sudoers、防火墙或 `/etc`。

## 三种安装方式先分清

RelaxKonOS 的服务端有三种安装方式，用途互不重叠。本页只讲**用户模式**。

| 方式 | 权限 | 适用场景 | 下载的包 |
| --- | --- | --- | --- |
| **用户模式** | 普通账号，**禁止 root** | 个人在已有账号下自建服务端 | `*-user-server.zip` |
| [系统模式](/docs/zh-CN/latest/getting-started/installation) | root / 管理员 | 多用户生产环境，注册系统服务 | `*-server.zip` |
| 开发者模式 | 需要 .NET 10 SDK | 参与 RelaxKonOS 开发 | 源码仓库 |

> **警告**：客户端包与服务端包不能混用。用户模式只接受 manifest 中 `packageKind` 为 `user-server` 的发布包。

### 用户模式与系统模式的差别

| 项目 | 用户模式 | 系统模式 |
| --- | --- | --- |
| 运行身份 | 你自己的账号 | Server / Guardian 系统服务账户 |
| 服务注册 | 无，由 `relaxkon` 命令管理进程 | systemd 服务或 Windows 服务 |
| 监听地址 | 仅 `127.0.0.1` | 可配置：仅本机 / 局域网 / 反向代理 |
| 权限助手 | 不安装，`Privileges: disabled` | 安装，并由固定的 sudoers 规则调用 |
| 主机改动 | 仅 XDG 目录 | `/etc`、systemd、sudoers、防火墙 |
| 升级 | `relaxkon upgrade`，就绪检查失败自动回滚 | 重新运行安装器 |
| 卸载 | `relaxkon uninstall` | 发布包内的卸载脚本 |

## 安装前准备

- 一个**普通（非 root）Linux 账号**。安装脚本与生命周期命令都会拒绝 root，`sudo` 反而会让安装失败。
- 系统命令：`bash`、`realpath`、`stat`、`find`、`sha256sum`，以及 `flock`（通常由 `util-linux` 提供）。缺少 `flock` 时安装会直接报错。
- 若从 HTTPS 发布地址在线安装，还需要 `curl` 与 `unzip`。
- 一份 `*-user-server.zip` 发布包及其官方公布的 SHA-256。
- 不需要 systemd，不需要 sudo，不需要 root。

> **提示**：官网[下载页](https://relaxkon.com/downloads)会列出稳定通道的包名、大小与校验和；离线服务器只需把服务端包复制过去。

## 分步安装

### 1. 解压发布包

以目标账号操作，**不要**加 `sudo`：

```bash
unzip RelaxKonOS-<version>-linux-x64-user-server.zip -d RelaxKonOS-user-server
```

### 2. 运行安装器

```bash
./RelaxKonOS-user-server/deployment/user/install-relaxkonos.sh \
  --mode user \
  --bundle ./RelaxKonOS-user-server
```

安装器会依次完成：

1. 校验 bundle 是否完整（`manifest.json`、`payload/`、`deployment/user/relaxkon`）；
2. 校验 `manifest.json` 的 `schemaVersion` 与 `packageKind: "user-server"`；
3. 校验包内不存在符号链接；
4. 用 `sha256sum` 校验全部文件，并确认文件清单与实际内容完全一致；
5. 把版本落到 `server/versions/<version>/`，再用 `server/current` 软链切换，并更新 `bin/relaxkon`。

也可以从官方发布地址在线安装。此时 `--release-uri` 必须是 HTTPS，且必须同时给出 64 位十六进制 SHA-256：

```bash
./deployment/user/install-relaxkonos.sh \
  --mode user \
  --release-uri https://<host>/relaxkonos/stable/<version>/linux-x64/server/<archive>.zip \
  --release-sha256 <64-hex-sha256>
```

### 3. 确认安装位置

用户模式只写入该账号的 XDG 目录，权限为 `0700` / `0600`：

| 用途 | 默认路径 |
| --- | --- |
| 程序与版本目录 | `${XDG_DATA_HOME:-$HOME/.local/share}/relaxkonos/` |
| 生命周期命令 | `${XDG_DATA_HOME:-$HOME/.local/share}/relaxkonos/bin/relaxkon` |
| 配置与密钥 | `${XDG_CONFIG_HOME:-$HOME/.config}/relaxkonos/`（`appsettings.user.json`、`secrets/guardian.secret`） |
| 运行状态 | `${XDG_STATE_HOME:-$HOME/.local/state}/relaxkonos/`（PID、控制套接字、`install-state.json`、SQLite 数据库） |
| 日志 | `${XDG_STATE_HOME:-$HOME/.local/state}/relaxkonos/logs/{server,guardian}.log` |
| 下载缓存 | `${XDG_CACHE_HOME:-$HOME/.cache}/relaxkonos/` |

安装器**不会**修改你的 `PATH`，也不会写入系统级可执行路径。

## 启动与验证

```bash
RELAXKON=""${XDG_DATA_HOME:-$HOME/.local/share}/relaxkonos/bin/relaxkon""

"$RELAXKON" start
"$RELAXKON" status
```

`status` 的期望输出：

```text
RelaxKonOS User Mode is running (pid <n>, loopback 127.0.0.1:5000).
```

它并不是简单地检查进程是否存在，而是通过当前用户私有的控制套接字（`…/relaxkonos/run/server.sock`，权限 `0600`）请求 `/ready`。只要套接字没有就绪，它会明确报告未就绪，而不是给出成功。

其他常用命令：

```bash
"$RELAXKON" start --foreground   # 前台运行，便于直接观察输出
"$RELAXKON" stop                 # 停止 Server 与 Guardian
```

服务端默认监听 `http://127.0.0.1:5000`，端口可以覆盖：

```bash
RELAXKONOS_PORT=5100 "$RELAXKON" start
```

### 从自己的电脑连接

用户模式只绑定回环地址，所以远程访问要走 SSH 本地转发，再把客户端指向本机地址：

```bash
ssh -L 5000:127.0.0.1:5000 <user>@<server>
```

## 升级

```bash
"$RELAXKON" upgrade --bundle ./RelaxKonOS-<new-version>-linux-x64-user-server
```

升级会停止服务、安装新版本、重新启动并等待就绪。**如果就绪检查失败，它会自动切回升级前的版本**，因此升级不会把你留在一个起不来的服务上。

> **注意**：同一个版本号不能被重复安装。升级时请使用新的版本号。

## 卸载

```bash
"$RELAXKON" uninstall
```

它会先停止服务，然后删除 data / config / state / cache 四个目录。

> **警告**：`uninstall` 会一并删除数据库、配置、密钥与日志，且**不可恢复**。如需保留，请先备份 `${XDG_STATE_HOME:-$HOME/.local/state}/relaxkonos/` 与 `${XDG_CONFIG_HOME:-$HOME/.config}/relaxkonos/`。

## 常见问题

| 现象 | 原因与处理 |
| --- | --- |
| `User Mode must not be installed as root.` | 用了 `sudo` 或已切到 root。请回到普通账号重新执行。 |
| `--mode user or --mode system is required.` | 漏写 `--mode user`，或在没有 `sudo` 的情况下写了 `--mode system`。 |
| `not a complete user-server bundle` | 解压的不是 `*-user-server` 包，或包不完整。 |
| `bundle is not a user-server manifest` | 包的 `manifest.json` 不是 `packageKind: "user-server"`。 |
| `bundle file checksum verification failed` | 文件损坏。重新下载并核对官方公布的 SHA-256。 |
| `another RelaxKonOS lifecycle operation is already running` | 另一处正在执行生命周期操作并持有 `…/relaxkonos/run/launcher.lock`。等它结束。 |
| `flock is required for safe User Mode lifecycle operations` | 系统缺少 `flock`（`util-linux`）。安装后重试。 |
| `status` 报进程在跑但控制套接字未就绪 | 查看 `logs/server.log`；通常是首次启动仍在初始化，或端口被占用。 |
| `version already installed: <version>` | 该版本已安装。换新版本号，或先 `uninstall`。 |

## 查看源码与反馈

用户模式安装器的完整实现可以直接阅读：

- 生命周期命令：`deployment/user/relaxkon`
- 用户模式入口：`deployment/user/install-relaxkonos.sh`
- 系统模式安装器（对照用）：`deployment/bootstrap/install-relaxkonos.sh`

源码仓库、Issue 与 Pull Request 都在 [nanaminato/RelaxKonOS](https://github.com/nanaminato/RelaxKonOS)。

## 下一步

- [快速开始](/docs/zh-CN/latest/getting-started/quick-start)
- [安装](/docs/zh-CN/latest/getting-started/installation)
- [安全模型](/docs/zh-CN/latest/concepts/security)
