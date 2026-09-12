# RelaxKon website API

`RelaxKonServer/RelaxKonServer` is a pure ASP.NET Core 10 REST API for the RelaxKon website. It does not host Razor pages, MVC views, static website files or the Angular client: it is the single source of truth for documentation and website content.

## Run locally

- Install the **.NET 10 SDK**.
- From this directory run `dotnet restore` and then `dotnet watch run`.
- The HTTPS launch profile uses `https://localhost:7252`; inspect `RelaxKonServer/Properties/launchSettings.json` if that ever changes.
- Trust the development certificate once with `dotnet dev-certs https --trust` (Windows/macOS). On Linux, import the certificate into the browser trust store instead.

OpenAPI is served at `/swagger/v1/swagger.json`, with a Swagger UI entry point at `/swagger`. A liveness probe is available at `/api/health`.

## Content layout

All website content is owned by the server and lives under `Content/`:

```text
Content/
├── Docs/{language}/{version}/…      # Markdown documentation tree
├── Releases/{version}.md            # Markdown release notes
├── Faq/{language}.json              # FAQ entries per language
└── Downloads/downloads.json         # Download descriptor
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
- **Group navigation**: `category` becomes a sidebar group; groups and documents are ordered by `order`.
- **Add a language**: create `Content/Docs/<code>/latest`. Language names for `zh-CN` and `ja-JP` are built in; other codes fall back to the code itself.
- **Add a version**: create another directory next to `latest`. `latest` sorts first.
- **Translation fallback**: if a slug is missing in the requested language it is served from `en-US`, and the response sets `isFallback` so the UI can say so.

### Releases, FAQ and downloads

- Release notes are Markdown files named after the version. Front matter supports `version`, `title`, `date`, `summary` and `prerelease`. The first bullets of the body become the response's `highlights`.
- `Faq/<language>.json` is an array of `{ question, answer, category, order }`.
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

`appsettings.Development.json` lowers the cache TTL so content edits show up quickly during development.

## Endpoints

| Method | Route | Purpose |
| --- | --- | --- |
| GET | `/api/health` | Liveness probe |
| GET | `/api/docs/languages` | Documentation languages |
| GET | `/api/docs/versions?language=` | Documentation versions |
| GET | `/api/docs/{language}/{version}/navigation` | Sidebar navigation tree |
| GET | `/api/docs/{language}/{version}/{slug}` | Document with headings and previous/next |
| GET | `/api/docs/search?q=&language=&version=` | Search titles, descriptions and content |
| GET | `/api/downloads` | Download descriptors |
| GET | `/api/releases` | Release summaries |
| GET | `/api/releases/{version}` | Release detail with Markdown body |
| GET | `/api/faq?language=` | FAQ entries |

## Caching and security

- `IMemoryCache` caches languages, versions, navigation indexes, documents, releases, FAQ and downloads. Development uses a short TTL; production uses a longer one. Cache invalidation can be layered on later.
- The document service accepts only logical segments, normalises the path against the controlled content root and rejects anything that escapes it, so directory traversal is not possible.
- Language, version, slug and search inputs are validated and length-limited.
- Responses carry `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `X-Permitted-Cross-Domain-Policies` and `Cross-Origin-Resource-Policy`. HTTPS redirection and HSTS are enabled in production.
- Unhandled exceptions are logged server side and returned as a generic problem response; stack traces and absolute paths are never exposed.
- Development CORS origins come from `appsettings.Development.json` and are never hard-coded in `Program.cs`. `AllowAnyOrigin` is not used.

## Project boundary

This API must not serve the Angular application. See [`../WEBSITE_ARCHITECTURE.md`](../WEBSITE_ARCHITECTURE.md) for all workspace boundary constraints.
