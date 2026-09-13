# RelaxKon website API

[中文](./README.md) · [日本語](./README.ja.md)

`RelaxKonServer/RelaxKonServer` is a pure **ASP.NET Core 10** REST API for the RelaxKon website. It does not host Razor pages, MVC views, static website files or the Angular client: it is the single source of truth for documentation and website content. Controlled API endpoints also stream RelaxKonOS releases, so a separate download project is unnecessary.

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
├── Releases/{version}.md            # Markdown release notes (file name is the version)
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
- **Group navigation**: `category` becomes a sidebar group; groups and documents are ordered by `order`, then by title.
- **Add a language**: create `Content/Docs/<code>/latest`. Languages are returned in the fixed order `en-US`, `zh-CN`, `ja-JP`; display names for `zh-CN` and `ja-JP` are built in (简体中文 / 日本語) and any other code falls back to the code itself.
- **Add a version**: create another directory next to `latest`. `latest` sorts first.
- **Translation fallback**: if a slug is missing in the requested language it is served from `en-US`, and the response sets `isFallback` so the UI can say so. Fallback entries keep the requested language's category labels so the navigation tree stays uniformly grouped.
- **Current state**: `en-US`, `zh-CN` and `ja-JP` each hold 26 documents and are fully aligned. **Keep the three languages at the same document count when you add or remove files**, otherwise fallback entries appear in the navigation.
- Language and version segments accept only `[A-Za-z0-9-]`; slugs additionally accept `/` and `_` up to 256 characters. Anything else returns 404.

### Releases, FAQ and downloads

- Release notes are Markdown files named after the version. Front matter supports `version`, `title`, `date`, `summary` and `prerelease`; when omitted, the file name is the version and the file's last write time is the date. The list is ordered by release date descending, and `highlights` in the detail response is the first six list items of the body.
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

`Content/ReleaseDelivery/` is deployment data served by the existing API, not a new web project. Versioned ZIPs are immutable-cached for one year; `latest` descriptors and installers use `no-cache`; GET, HEAD and Range resumption are supported. A release maintainer first runs:

```powershell
./deployment/Publish-RelaxKonOSRelease.ps1 `
  -SourceDirectory 'D:\artifacts\relaxkonos' `
  -BootstrapDirectory '..\RelaxKonOS\deployment\bootstrap'
```

It verifies ZIP SHA-256 values, places files under `stable/{version}/{runtime}/`, produces `latest/{runtime}.json`, and updates the existing `/api/downloads` list so the website's offline-package cards need no manual maintenance. `-PublicBaseUri https://relaxkon.com` makes the main site canonical; the default is `https://downloads.relaxkon.com`. Both names serve one deployment.

Deploy `deployment/nginx/relaxkon.com.conf`, point `relaxkon.com`, `www.relaxkon.com`, and `downloads.relaxkon.com` at one server, and configure a certificate covering every name. `/api/` and `/relaxkonos/` are proxied to this API; the existing Angular build handles all other paths.

Users can use either domain; the installer fetches the stable descriptor and verifies ZIP SHA-256:

```bash
# Linux: interactive / unattended
curl -fsSL https://downloads.relaxkon.com/relaxkonos/stable/latest/bootstrap/install-relaxkonos.sh | sudo bash
curl -fsSL https://relaxkon.com/relaxkonos/stable/latest/bootstrap/install-relaxkonos.sh | sudo bash -s -- --non-interactive
```

```powershell
# Windows PowerShell: interactive / unattended
irm https://downloads.relaxkon.com/relaxkonos/stable/latest/install.ps1 | iex
& ([scriptblock]::Create((irm 'https://relaxkon.com/relaxkonos/stable/latest/install.ps1'))) -InstallerArguments '-NonInteractive'
```

`install.ps1` stages the real Windows installer on disk first, so it can safely restart during UAC elevation. Offline ZIPs and explicitly supplied release URI + SHA-256 remain supported.

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
