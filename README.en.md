# RelaxKon website API

[中文](./README.md) · [日本語](./README.ja.md)

Website <https://relaxkon.com> · Product source repository <https://github.com/nanaminato/RelaxKonOS>

`RelaxKonServer/RelaxKonServer` provides the website REST API using **ASP.NET Core 10** and is the sole source of documentation and site content. The separate website project serves frontend pages and static assets; this server does not host Razor, MVC or Angular pages. Controlled download endpoints stream RelaxKonOS release packages.

## Run locally

- Install the **.NET 10 SDK**.
- From this directory run `dotnet restore` and then `dotnet watch run`.
- Launch profiles live in `RelaxKonServer/Properties/launchSettings.json`: the `https` profile listens on both `https://localhost:7252` and `http://localhost:5062`, the `http` profile only on `5062`.
- Trust the development certificate once with `dotnet dev-certs https --trust` (Windows/macOS). On Linux, import the certificate into the browser trust store instead.

The OpenAPI description is served at `/swagger/v1/swagger.json`, with a Swagger UI entry point at `/swagger` and a liveness probe at `/api/health`.

> **Known limitation**: `/swagger` returns an HTML page, but that page also carries the global CSP set by the security middleware (`default-src 'none'`), so a browser will not load the Swagger UI assets from unpkg and the page does not work as-is. Fetching `swagger.json` from a command line is unaffected; restoring the UI requires relaxing CSP for that path only.

## Content layout

All website content is owned by the server and lives under `Content/`:

```text
Content/
├── Docs/{language}/{version}/…      # Markdown documentation tree
├── Releases/{version}.md            # Shared release metadata and artifacts
├── Releases/{language}/{version}.md # Localized release text
├── Faq/{language}.json              # FAQ entries per language
├── Downloads/downloads.json         # Download descriptor
└── ReleaseDelivery/…                # Deployed RelaxKonOS ZIPs, checksums, descriptors and bootstrap scripts
```

### Documentation

Markdown files optionally start with YAML-style front matter:

```markdown
---
title: Terminal
description: Use a persistent remote terminal session.
category: Applications
order: 14
---

# Terminal
```

- **Add a document**: create a `.md` file anywhere under the version directory. The slug is the path without `.md` (`apps/terminal`).
- **Group navigation**: `category` becomes a sidebar group. Groups are ordered by the smallest `order` inside them, and documents within a group by `order` then title. Note that a page's previous/next links come from sorting **all** documents by `order` alone, so `order` must be unique across the entire document set, not just within one group — duplicates make paging jump to an unrelated page. `tools/verify-doc-order.mjs` checks this, and `tools/verify-doc-links.mjs` checks every internal link and front matter block.
- **Add a language**: create `Content/Docs/<code>/latest`. Languages are returned in the fixed order `en-US`, `zh-CN`, `ja-JP`; display names for `zh-CN` and `ja-JP` are built in (简体中文 / 日本語) and any other code falls back to the code itself.
- **Add a version**: create another directory next to `latest`. `latest` sorts first.
- **Translation fallback**: if a slug is missing in the requested language it is served from `en-US`, and the response sets `isFallback` so the UI can say so. Fallback entries keep the requested language's category labels so the navigation tree stays uniformly grouped.
- **Current state**: `en-US`, `zh-CN` and `ja-JP` each hold 46 documents (9 getting-started + 9 concepts + 28 applications) and are fully aligned. **Keep the three languages at the same document count when you add or remove files**, otherwise fallback entries appear in the navigation. Afterwards run `node tools/verify-doc-order.mjs` (counts, `order` uniqueness, language parity) and `node tools/verify-doc-links.mjs` (internal links and front matter).
- Language and version segments accept only `[A-Za-z0-9-]`; slugs additionally accept `/` and `_` up to 256 characters. Anything else returns 404.

### Releases, FAQ and downloads

- Release notes use `Releases/{version}.md` for shared `version`, `date`, `prerelease` and artifact inventory, plus `Releases/{language}/{version}.md` for translated `title`, `summary` and body. Shared Markdown can use `{artifactsTitle}`, `{packageLabel}` and `{downloadsLabel}`, supplied by translation front matter. Version, date, download URLs, sizes and SHA-256 values are maintained only in the shared file. Both release endpoints accept `?language=zh-CN|en-US|ja-JP`, defaulting to English. Missing translations fall back to English and return the actual `language` and `isFallback`; every release requires an English file. Lists sort by date descending; `highlights` uses the first six list items of the translated body, excluding shared artifacts. After building, run `node tools/verify-release-localization.mjs` to verify localization, shared data and fallback.
- `Faq/<language>.json` is an array of `{ question, answer, category, order }`. A missing or malformed language falls back to `en-US`, and an empty `category` becomes `General`.
- `Downloads/downloads.json` is an array of `{ platform, architecture, version, url, size, checksum, releaseDate, isAvailable, fileName }`.

### Options

Strongly typed options are bound from configuration; controllers never read raw configuration strings.

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

`appsettings.Development.json` lowers the cache TTL to two minutes so content edits show up quickly during development, and supplies the development CORS origins. The configured TTL is clamped to 1–120 minutes in code.

## Endpoints

| Method | Route | Purpose |
| --- | --- | --- |
| GET | `/api/health` | Liveness probe |
| GET | `/api/docs/languages` | Documentation languages |
| GET | `/api/docs/versions?language=` | Documentation versions (all languages merged when `language` is omitted) |
| GET | `/api/docs/{language}/{version}/navigation` | Sidebar navigation tree grouped by `category` |
| GET | `/api/docs/{language}/{version}/{slug}` | Document body with `headings`, `previous`/`next` and `isFallback` |
| GET | `/api/docs/search?q=&language=&version=` | Search titles, descriptions and content (`language` defaults to `en-US`, `version` to `latest`) |
| GET | `/api/downloads` | Download descriptors |
| GET | `/api/releases` | Release summaries |
| GET | `/api/releases/{version}` | Release detail with Markdown body |
| GET | `/api/faq?language=` | FAQ entries for a language (`en-US` by default) |
| GET/HEAD | `/relaxkonos/{artifact}` | RelaxKonOS ZIPs, checksums, descriptors and bootstrap scripts; supports HTTP Range |

Search details: a `q` shorter than two characters returns 400; content matches return a `snippet` that is plain text with the Markdown syntax stripped; at most 20 results are returned, with title matches first.

## RelaxKonOS delivery and one-command installation

`Content/ReleaseDelivery/` is deployment data served by the existing API, not a new web project. Versioned ZIPs are immutable-cached for one year; `latest` descriptors use `no-cache`; GET, HEAD and Range resumption are supported. A release maintainer first runs:

```powershell
./deployment/Publish-RelaxKonOSRelease.ps1 `
  -SourceDirectory 'D:\artifacts\relaxkonos'
```

It verifies ZIP SHA-256 values, places files under `stable/{version}/{runtime}/`, produces `latest/{runtime}.json`, and updates the existing `/api/downloads` list so the website's offline-package cards need no manual maintenance. `-PublicBaseUri https://relaxkon.com` makes the main site canonical; the default is `https://downloads.relaxkon.com`. Both names serve one deployment.

Deploy `deployment/nginx/relaxkon.com.conf`, point `relaxkon.com`, `www.relaxkon.com`, and `downloads.relaxkon.com` at one server, and configure a certificate covering every name. `/api/` and `/relaxkonos/` are proxied to this API; the existing Angular build handles all other paths.

Users install through Server Center in the client with the official source selected. The client uploads its embedded deployment launcher; the host downloads the stable descriptor and ZIP, verifies checksums, and invokes the deployment engines inside the ZIP for installation, upgrades, and removal. The website no longer serves command-line installation or uninstallation scripts.

## Backend release package

The repository previously contained `Publish-RelaxKonOSRelease.ps1`, which imports RelaxKonOS artifacts into this API's delivery directory, but no script for packaging the API itself. `deployment/New-RelaxKonServerPackage.ps1` now creates a self-contained server ZIP, its SHA-256 file, and a release descriptor that can be consumed by that publishing script.

```powershell
./deployment/New-RelaxKonServerPackage.ps1 `
  -Version 0.1.0 `
  -Runtime linux-x64 `
  -OutputDirectory artifacts `
  -ArtifactBaseUri https://downloads.relaxkon.com/releases
```

The ZIP extracts to the application working directory; start `./RelaxKonServer` there (`RelaxKonServer.exe` on Windows). A self-contained package does not require .NET on the target host. Configure HTTPS through systemd, a Windows Service, or a reverse proxy in production, and do not treat development certificates or `appsettings.Development.json` as production secrets.

## Caching and security

- `IMemoryCache` caches languages, versions, navigation indexes, documents, releases, FAQ and downloads. Development uses a short TTL, production a longer one. Cache invalidation can be layered on later.
- The document service accepts only logical segments, normalises the path against the controlled content root and rejects anything that escapes it, so directory traversal is not possible.
- Language, version, slug and search inputs are validated and length-limited.
- Every response carries `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `X-Permitted-Cross-Domain-Policies`, `Cross-Origin-Resource-Policy`, `Content-Security-Policy` and `Permissions-Policy`. HTTPS redirection and HSTS (outside development) are enabled, along with response compression.
- Unhandled exceptions are logged server side and returned as a generic ProblemDetails response; stack traces and absolute paths are never exposed.
- Development CORS origins come from `appsettings.Development.json` and are never hard-coded in `Program.cs`. `AllowAnyOrigin` is not used.

## Smoke test

`smoke-test.ps1` in the repository root starts the API on the `http` profile (`http://localhost:5062`), requests the health probe, documentation navigation, documents in all three languages, releases, downloads and FAQ, and writes the results to `api-smoke.log`. Run it after changing endpoints or the content layout.

## Project boundary

This API must not serve the Angular application. See [`../WEBSITE_ARCHITECTURE.en.md`](../WEBSITE_ARCHITECTURE.en.md) for all workspace boundary constraints.

## Content updates and verification

Guides distinguish current source, actual verification and published packages. Detailed Android specifications remain in the product project at `Client/RelaxKonOS.Client.Android/docs/`; the website provides user-facing summaries and links. Android, account sign-in, upload resumption, alerts and recovery guides are included, with 16 FAQ entries. The 0.1.2 release record derives from four existing artifacts, not an inferred changelog for recent source features.

```bash
node tools/verify-doc-order.mjs
node tools/verify-doc-links.mjs
node tools/verify-release-inventory.mjs
dotnet build RelaxKonServer/RelaxKonServer.csproj --no-restore
```
