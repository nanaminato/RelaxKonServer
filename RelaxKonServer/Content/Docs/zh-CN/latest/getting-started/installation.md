---
title: 安装
description: 安装 RelaxKonOS 客户端与服务端的准备工作与步骤。
category: 开始使用
order: 2
---

# 安装

RelaxKonOS 由**客户端**与**服务端**两部分组成。客户端安装在你每天使用的设备上，服务端运行在你希望长期保持工作区的机器上。

## 前置要求

- **.NET 10.0 SDK** 或更高版本
- 客户端操作系统：Windows 10/11、macOS、Ubuntu 20.04+
- 服务端操作系统：Ubuntu 20.04+ 或 Windows Server 2016+

## 先选择服务端安装方式

| 方式 | 权限 | 说明 |
| --- | --- | --- |
| [用户模式](/docs/zh-CN/latest/getting-started/user-mode) | 不需要 sudo | 在普通 Linux 账号下运行服务端，只监听 `127.0.0.1`，不改动系统目录 |
| 系统模式 | root / 管理员 | 用一键安装器注册系统服务与权限助手，适合多用户生产环境 |
| 从源码运行 | .NET 10 SDK | 本页下面的步骤，只适合开发与调试 |

> **注意**：下面的步骤需要 .NET SDK 并直接运行源码，**不是**面向普通用户的安装方式。想直接部署服务端，请先阅读[用户模式安装](/docs/zh-CN/latest/getting-started/user-mode)或官网[下载页](https://relaxkon.com/downloads)。

## 宿主授权与特权助手

服务端**不以 root 或管理员身份运行**。所有需要宿主特权的操作都通过一个专用的特权助手执行，它只接受固定的、结构化的动作，不提供通用命令执行入口。安装时请留意以下几点，它们决定了后续哪些功能可用。

### 管理员权限是前提

以系统模式安装时，安装器会创建专用的服务账户、把助手的发布目录与策略文件设为服务账户不可写，并只允许服务账户以无参数方式调用助手。因此：

- **发现与只读功能**在服务进程可读时即可使用。
- **会改变宿主状态的功能**（安装运行时、写系统配置、控制系统服务、部署证书等）要求 RelaxKonOS 具备足够权限；权限不足时界面返回明确的问题码，并提示以更高权限重新部署，而不是失败得无声无息。
- 客户端**不会**收集 sudo、管理员或服务账户口令，也不会把请求参数拼接成 shell 命令。遇到权限不足时，正确处理方式是重新以所需权限安装或启动服务端。

### Docker 访问需要显式选择

Docker 守护进程套接字的控制权接近 root 权限，因此安装默认**不会**把服务账户加入相应权限组。若确定要让 [Docker 管理器](/docs/zh-CN/latest/apps/docker)管理本机引擎，必须在系统模式安装时显式追加 `--docker-access`：

```bash
sudo deployment/bootstrap/install-relaxkonos.sh --mode system --bundle /path/to/release --docker-access
```

- 这项选择会写入仅 root 可读的策略文件；若 Docker 已安装，安装器会授予访问权限并重启服务端。
- 若 Docker 由 RelaxKonOS 之后安装，助手会在安装后执行同一固定授权，并把该任务标记为「需要重启」；重启服务端后再刷新 Docker 状态即可。
- **未选择该选项时，Docker 安装会在修改主机之前拒绝执行**，而不是先改一半再失败。
- 用户模式默认不支持 Docker，即使服务账户能访问套接字也会报告为等价 root 的风险并要求单独确认。

### 助手不可用时的表现

助手缺失、被卸载或调用被拒绝时，相关功能会以稳定问题码失败并说明原因，例如代理或 Docker 的提权类操作无法完成。此时**已运行的工作负载不会被中断**，已保存的配置也不会被回滚——失败发生在动到宿主之前或之后都有明确记录，可按提示修复后重试。

## 从源码运行服务端

```bash
cd RelaxKonOS.Server
dotnet run
```

生产环境请务必修改 `appsettings.json` 中的 `Jwt:Secret`（至少 32 位随机字符串），并通过反向代理启用 HTTPS。

## 安装客户端

```bash
cd Client/RelaxKonOS.Client.Desktop
dotnet run
```

客户端启动后会显示登录窗口，输入**宿主系统**的用户名与密码即可登录。

登录窗口也可以选择 **SSH**，在主机尚未运行 RelaxKonOS Server 时直接连接。SSH 模式会确认主机密钥，并提供精简的维护桌面；它不会创建 RelaxKonOS 工作区。使用它安装或维护服务端前，请先阅读[服务器中心](/docs/zh-CN/latest/apps/server-center)。

## 首次登录

1. 打开客户端，进入登录窗口
2. 输入服务端所在宿主 OS 的用户凭据
3. 登录成功后进入桌面，Workspace 会自动创建并同步

> 身份由宿主操作系统校验（Windows LogonUser / Linux PAM + NSS），RelaxKonOS 不保存你的密码。

## 下一步

- [用户模式安装（Linux）](/docs/zh-CN/latest/getting-started/user-mode)
- [快速开始](/docs/zh-CN/latest/getting-started/quick-start)
- [服务器中心](/docs/zh-CN/latest/apps/server-center)
- [安全模型](/docs/zh-CN/latest/concepts/security)
- [Docker 管理器](/docs/zh-CN/latest/apps/docker)与[代理管理器](/docs/zh-CN/latest/apps/proxy-manager)（需要宿主授权）
- 源码与 Issue：[nanaminato/RelaxKonOS](https://github.com/nanaminato/RelaxKonOS)
