# RelaxKon 官网后端 API

[English](./README.en.md) · [日本語](./README.ja.md)

`RelaxKonServer/RelaxKonServer` 是 RelaxKon 官方网站的纯 **ASP.NET Core 10** REST API。它不托管 Razor 页面、MVC 视图、静态网站文件，也不托管 Angular 客户端 —— 它是文档与站点内容的唯一来源。RelaxKonOS 发布物也由受控 API 下载端点流式返回，无需新建下载项目。

## 本地运行

- 安装 **.NET 10 SDK**。
- 在本目录执行 `dotnet restore`，然后 `dotnet watch run`。
- 启动配置见 `RelaxKonServer/Properties/launchSettings.json`：`https` 配置文件同时监听 `https://localhost:7252` 与 `http://localhost:5062`，`http` 配置文件只监听 `5062`。
- 首次使用执行 `dotnet dev-certs https --trust`（Windows/macOS）。Linux 上请改为把证书导入浏览器信任库。

OpenAPI 描述文件位于 `/swagger/v1/swagger.json`，Swagger UI 入口在 `/swagger`，存活探针在 `/api/health`。

> **已知限制**：`/swagger` 返回的是 HTML 页面，但它同样会带上全局安全中间件设置的 CSP（`default-src 'none'`），因此浏览器不会加载 unpkg 上的 Swagger UI 资源，页面实际不可用。用命令行取 `swagger.json` 不受影响；若要恢复 UI，需要为该路径单独放宽 CSP。

## 内容布局

所有站点内容都由后端拥有，位于 `Content/` 之下：

```text
Content/
├── Docs/{language}/{version}/…      # Markdown 文档树
├── Releases/{version}.md            # Markdown 发布说明（文件名即版本号）
├── Faq/{language}.json              # 各语言 FAQ
├── Downloads/downloads.json         # 下载描述文件
└── ReleaseDelivery/…                # 已发布的 RelaxKonOS ZIP、校验和、描述与安装器（部署数据）
```

### 文档

Markdown 文件可以选择性地以 YAML 风格 front matter 开头：

```markdown
---
title: Terminal
description: Use a persistent remote terminal session.
category: Applications
order: 14
---

# Terminal
```

- **新增文档**：在版本目录下的任意位置创建 `.md` 文件，slug 就是去掉 `.md` 的路径（`apps/terminal`）。
- **导航分组**：`category` 成为侧边栏分组；分组与文档都按 `order` 排序，其次按标题排序。
- **新增语言**：创建 `Content/Docs/<code>/latest`。语言目录按 `en-US`、`zh-CN`、`ja-JP` 的固定顺序返回，`zh-CN` 与 `ja-JP` 的显示名称为内置（简体中文 / 日本語），其他代码回退为代码本身。
- **新增版本**：在 `latest` 同级再建一个目录；`latest` 排在最前。
- **翻译回退**：目标语言缺少某个 slug 时改用 `en-US` 的版本，并在响应中把 `isFallback` 置为 `true`；回退项的分类名仍按目标语言本地化，以保证导航分组标题统一。
- **当前状态**：`en-US`、`zh-CN`、`ja-JP` 各 26 篇且完全对齐。**增删内容文件时必须让三种语言的篇数保持一致**，否则导航会出现回退项。
- slug 与语言/版本段只允许 `[A-Za-z0-9-]`，slug 额外允许 `/` 与 `_`，最长 256 字符；不符合规则的请求返回 404。

### 发布说明、FAQ 与下载

- 发布说明是按版本命名的 Markdown 文件，front matter 支持 `version`、`title`、`date`、`summary`、`prerelease`；缺省时用文件名当版本号、文件修改时间当日期。列表按发布日期倒序，详情响应中的 `highlights` 取自正文的前 6 个列表项。
- `Faq/<language>.json` 是 `{ question, answer, category, order }` 数组；该语言文件缺失或语言代码非法时回退 `en-US`，`category` 为空时补 `General`。
- `Downloads/downloads.json` 是 `{ platform, architecture, version, url, size, checksum, releaseDate, isAvailable, fileName }` 数组。

### 配置项

强类型选项从配置绑定，控制器从不直接读取原始配置字符串。

```json
{
  "Documentation": { "RootPath": "Content/Docs", "CacheMinutes": 30 },
  "Content": {
    "RootPath": "Content",
    "ReleasesPath": "Releases",
    "FaqPath": "Faq",
    "DownloadsPath": "Downloads/downloads.json",
    "CacheMinutes": 30
  }
}
```

`appsettings.Development.json` 把缓存 TTL 降到 2 分钟，让内容改动在开发时快速生效；该文件同时提供开发 CORS 允许的来源。缓存 TTL 在代码中会被裁剪到 1–120 分钟之间。

## 端点

| 方法 | 路由 | 说明 |
| --- | --- | --- |
| GET | `/api/health` | 存活探针 |
| GET | `/api/docs/languages` | 文档语言列表 |
| GET | `/api/docs/versions?language=` | 文档版本列表（省略 `language` 时合并所有语言） |
| GET | `/api/docs/{language}/{version}/navigation` | 侧边导航树（按 `category` 分组） |
| GET | `/api/docs/{language}/{version}/{slug}` | 文档正文、`headings`、`previous`/`next`、`isFallback` |
| GET | `/api/docs/search?q=&language=&version=` | 搜索标题、描述与正文（`language` 默认 `en-US`，`version` 默认 `latest`） |
| GET | `/api/downloads` | 下载描述 |
| GET | `/api/releases` | 发布说明摘要列表 |
| GET | `/api/releases/{version}` | 单条发布说明，含 Markdown 正文 |
| GET | `/api/faq?language=` | 指定语言的 FAQ 条目（默认 `en-US`） |
| GET/HEAD | `/relaxkonos/{artifact}` | RelaxKonOS ZIP、校验和、描述和引导安装器；支持 HTTP Range 续传 |

搜索细节：`q` 少于 2 个字符返回 400；正文命中时返回的 `snippet` 是剥离 Markdown 语法后的纯文本摘要，最多 20 条结果，标题命中的排在前面。

## RelaxKonOS 下载与一键安装

`Content/ReleaseDelivery/` 是现有 API 提供的部署数据，不是新的 Web 项目。版本 ZIP 缓存一年且不可变；`latest` 描述与安装器使用 `no-cache`；允许 GET、HEAD 和 Range 续传。发布维护者先执行：

```powershell
./deployment/Publish-RelaxKonOSRelease.ps1 `
  -SourceDirectory 'D:\artifacts\relaxkonos' `
  -BootstrapDirectory '..\RelaxKonOS\deployment\bootstrap'
```

它会验证 ZIP 的 SHA-256，将文件放到 `stable/{version}/{runtime}/`，生成 `latest/{runtime}.json`，并同步更新现有 `/api/downloads` 清单，所以官网的离线包卡片无需人工维护。`-PublicBaseUri https://relaxkon.com` 可让主站成为规范 URL；默认 URL 是 `https://downloads.relaxkon.com`。两者由同一个网站部署提供服务。

部署 `deployment/nginx/relaxkon.com.conf` 后，让 `relaxkon.com`、`www.relaxkon.com`、`downloads.relaxkon.com` 指向同一台服务器，并配置覆盖全部名称的证书。`/api/`、`/relaxkonos/` 反向代理到本 API，其余请求继续由现有 Angular 构建产物处理。

用户可任选其中一个域名，安装器会下载稳定版描述并验证 ZIP SHA-256：

```bash
# Linux：交互式 / 无人值守
curl -fsSL https://downloads.relaxkon.com/relaxkonos/stable/latest/bootstrap/install-relaxkonos.sh | sudo bash
curl -fsSL https://relaxkon.com/relaxkonos/stable/latest/bootstrap/install-relaxkonos.sh | sudo bash -s -- --non-interactive
```

```powershell
# Windows PowerShell：交互式 / 无人值守
irm https://downloads.relaxkon.com/relaxkonos/stable/latest/install.ps1 | iex
& ([scriptblock]::Create((irm 'https://relaxkon.com/relaxkonos/stable/latest/install.ps1'))) -InstallerArguments '-NonInteractive'
```

Windows 的 `install.ps1` 会先把真正的安装器保存到临时目录，因而 UAC 提升可以安全地重新启动它。已有安装器仍支持离线 ZIP，或手工指定发布 URI 和 SHA-256。

## 缓存与安全

- `IMemoryCache` 缓存语言、版本、导航索引、文档、发布说明、FAQ 与下载。开发用短 TTL，生产用长 TTL。后续可以再叠加缓存失效机制。
- 内容服务只接受逻辑路径段，并将其相对受控内容根做规范化，越出根目录的请求一律拒绝，因此不存在目录穿越。
- 语言、版本、slug 与搜索输入都会被校验并限制长度。
- 响应统一携带 `X-Content-Type-Options`、`X-Frame-Options`、`Referrer-Policy`、`X-Permitted-Cross-Domain-Policies`、`Cross-Origin-Resource-Policy`、`Content-Security-Policy` 与 `Permissions-Policy`；生产环境启用 HTTPS 重定向与 HSTS（非开发环境），并开启响应压缩。
- 未处理异常在服务端记录日志，对外只返回统一的 ProblemDetails 响应；堆栈与绝对路径不会外泄。
- 开发 CORS 来源来自 `appsettings.Development.json`，绝不在 `Program.cs` 中硬编码，也不使用 `AllowAnyOrigin`。

## 冒烟测试

仓库根目录的 `smoke-test.ps1` 会用 `http` 配置（`http://localhost:5062`）把 API 拉起来，逐个请求健康检查、文档导航、三语正文、发布说明、下载与 FAQ，结果写入 `api-smoke.log`。改动端点或内容结构后建议跑一遍。

## 项目边界

本 API 不得托管 Angular 应用。工作区全部边界约束见 [`../WEBSITE_ARCHITECTURE.md`](../WEBSITE_ARCHITECTURE.md)。
