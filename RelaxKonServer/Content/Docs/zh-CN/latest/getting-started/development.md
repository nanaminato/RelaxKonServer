---
title: 从源码开发与调试
description: 配置 .NET 与 Angular 环境，构建源码并维护 NuGet/npm 依赖。
category: 开始使用
order: 7
---

# 从源码开发与调试

本教程面向修改源码的开发者。安装现成客户端和服务端请使用[安装指南](/docs/zh-CN/latest/getting-started/installation)。

## 环境与依赖基线

| 工程 | 环境与当前依赖 |
| --- | --- |
| RelaxKonOS 桌面与服务端 | .NET 10 SDK；Avalonia 核心包 12.1.3；DataGrid 12.1.2；Microsoft.OpenApi 2.12.2 |
| 官网与发布工具前端 | Angular 22.2.1；TypeScript 6.0.3；RxJS 7.8.2；Vitest 5.0.3；jsdom 30.1.2 |
| 官网 API 与发布工具 API | .NET 10 SDK |

前端使用 Node.js 22.22.3 以上的 22.x、24.15.0 以上的 24.x，或 26 以上版本，包管理器基线为 npm 11.19.1。Angular 构建器要求 TypeScript `>=6.0 <6.1`，不要直接升级到 TypeScript 7。配套 Angular 包需要保持一致；以仓库的 `package.json` 和 `package-lock.json` 为准。参考 [Angular 兼容矩阵](https://angular.dev/reference/versions)。

## 运行 RelaxKonOS

从 [RelaxKonOS 源码仓库](https://github.com/nanaminato/RelaxKonOS)获取源码，在仓库根目录执行：

```powershell
dotnet restore RelaxKonOS.sln
dotnet build RelaxKonOS.sln -c Release -m:1
```

分别在三个终端中启动 Guardian、Server 和桌面客户端：

```powershell
$env:RELAXKONOS_GUARDIAN_PIPE = 'relaxkonos-guardian-dev'
$env:RELAXKONOS_GUARDIAN_SHARED_SECRET = 'dev-guardian-secret-local-only'
dotnet run --project RelaxKonOS.Guardian.Agent
dotnet run --project RelaxKonOS.Server --launch-profile http
dotnet run --project Client/RelaxKonOS.Client.Desktop
```

本地客户端连接 `http://localhost:5090`。普通文件、终端和 Git 调试使用开发配置的 `local-identity` 后端，Server 与登录账户应为同一宿主身份。常规调试无需安装系统服务；受保护文件、真实防火墙或受管运行时需要单独配置特权 Helper，见源码中的[开发调试指南](https://github.com/nanaminato/RelaxKonOS/blob/master/docs/development/RelaxKonOS.Develop.md)。

Android 是独立 Kotlin/Compose 工程，构建步骤见[移动端指南](/docs/zh-CN/latest/getting-started/android)。

## 运行官网

分别获取 [RelaxKon 前端](https://github.com/nanaminato/RelaxKon)和 [RelaxKonServer 后端](https://github.com/nanaminato/RelaxKonServer)。在后端仓库根目录运行：

```powershell
dotnet run --project RelaxKonServer --launch-profile https
```

在前端仓库根目录运行：

```powershell
npm ci
npm start
```

打开 `http://localhost:4200`。`proxy.conf.json` 默认将 `/api` 代理到 `https://localhost:7252`；修改代理后重启开发服务器。首次本地 HTTPS 调试可执行 `dotnet dev-certs https --trust`，Linux 需自行导入浏览器信任库。验证生产构建和测试：

```powershell
npm run build
npm test -- --watch=false
```

官网文档源文件位于后端 `RelaxKonServer/Content/Docs/{language}/latest/`。中、英、日保持相同 slug 与唯一的 `order`，修改后在后端仓库执行 `node tools/verify-doc-order.mjs` 与 `node tools/verify-doc-links.mjs`。

## 维护依赖

- NuGet 版本集中维护在 RelaxKonOS 根目录的 `Directory.Packages.props`；子项目引用不填写版本。先执行 `dotnet list RelaxKonOS.sln package --outdated`，再修改兼容版本并重新构建。Avalonia 核心包同步更新，DataGrid 等独立发行组件按自己的稳定版更新。
- ASP.NET Core OpenAPI 10.0.x 要求 Microsoft.OpenApi 小于 3；ImageSharp 当前保留 3.1.12，跨主版本更新前核对 API 与 `THIRD_PARTY_NOTICES.md` 中的许可条件。
- npm 依赖分别维护在官网与发布工具的 `package.json`。先执行 `npm outdated`，修改版本后执行 `npm update`，同时提交锁文件，再执行构建和测试；新检出使用 `npm ci`。不要用 `--force` 或 `--legacy-peer-deps` 绕过兼容冲突。
- 发布工具前端使用相同依赖基线，仅供本机维护者使用，开发端口为 4201；构建步骤相同。

## 下一步

- [架构](/docs/zh-CN/latest/concepts/architecture)
- [应用模型](/docs/zh-CN/latest/concepts/application-model)
