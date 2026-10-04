---
title: Develop and debug from source
description: Set up .NET and Angular, build from source and maintain NuGet/npm dependencies.
category: Getting Started
order: 7
---

# Develop and debug from source

This tutorial is for source development. For distributed client and server packages, follow [Installation](/docs/en-US/latest/getting-started/installation).

## Environment and dependency baseline

| Project | Environment and current dependencies |
| --- | --- |
| RelaxKonOS desktop and server | .NET 10 SDK; Avalonia core packages 12.1.3; DataGrid 12.1.2; Microsoft.OpenApi 2.12.2 |
| Website and Publisher frontends | Angular 22.2.1; TypeScript 6.0.3; RxJS 7.8.2; Vitest 5.0.3; jsdom 30.1.2 |
| Website and Publisher APIs | .NET 10 SDK |

Use Node.js 22.x starting at 22.22.3, 24.x starting at 24.15.0, or version 26 and later. The package manager baseline is npm 11.19.1. The Angular builder requires TypeScript `>=6.0 <6.1`; do not upgrade directly to TypeScript 7. Keep Angular companion packages aligned and follow the repository's `package.json` and `package-lock.json`. See the [Angular compatibility table](https://angular.dev/reference/versions).

## Run RelaxKonOS

Get the [RelaxKonOS source](https://github.com/nanaminato/RelaxKonOS) and run from its repository root:

```powershell
dotnet restore RelaxKonOS.sln
dotnet build RelaxKonOS.sln -c Release -m:1
```

Start Guardian, Server and the desktop client in three separate terminals:

```powershell
$env:RELAXKONOS_GUARDIAN_PIPE = 'relaxkonos-guardian-dev'
$env:RELAXKONOS_GUARDIAN_SHARED_SECRET = 'dev-guardian-secret-local-only'
dotnet run --project RelaxKonOS.Guardian.Agent
dotnet run --project RelaxKonOS.Server --launch-profile http
dotnet run --project Client/RelaxKonOS.Client.Desktop
```

Connect locally to `http://localhost:5090`. Ordinary file, terminal and Git debugging uses the development `local-identity` backend: the Server process and sign-in account should have the same host identity. Routine debugging needs no installed system services. Protected files, real firewall changes and managed runtimes require a separately configured privileged Helper; see the source [development guide](https://github.com/nanaminato/RelaxKonOS/blob/master/docs/development/RelaxKonOS.Develop.md). Android is a separate Kotlin/Compose project; see the [mobile guide](/docs/en-US/latest/getting-started/android).

## Run the website

Get the [RelaxKon frontend](https://github.com/nanaminato/RelaxKon) and [RelaxKonServer backend](https://github.com/nanaminato/RelaxKonServer). In the backend repository root, run:

```powershell
dotnet run --project RelaxKonServer --launch-profile https
```

In the frontend repository root, run:

```powershell
npm ci
npm start
```

Open `http://localhost:4200`. By default, `proxy.conf.json` forwards `/api` to `https://localhost:7252`; restart the development server after changing the proxy. For initial local HTTPS setup, use `dotnet dev-certs https --trust`; on Linux, import the certificate into your browser's trust store. Verify production builds and tests:

```powershell
npm run build
npm test -- --watch=false
```

Website documentation lives under the backend's `RelaxKonServer/Content/Docs/{language}/latest/`. Keep Chinese, English and Japanese slugs aligned and `order` values unique. Run `node tools/verify-doc-order.mjs` and `node tools/verify-doc-links.mjs` from the backend repository after editing.

## Maintain dependencies

- NuGet versions are centralized in RelaxKonOS's `Directory.Packages.props`; project references omit versions. Run `dotnet list RelaxKonOS.sln package --outdated`, update compatible versions and rebuild. Align Avalonia core packages; independently released components such as DataGrid follow their own stable versions.
- ASP.NET Core OpenAPI 10.0.x requires Microsoft.OpenApi below 3. ImageSharp stays at 3.1.12; review API changes and the licensing conditions in `THIRD_PARTY_NOTICES.md` before a major upgrade.
- Maintain npm dependencies in each frontend's `package.json`. Run `npm outdated`, edit versions, run `npm update` and commit the lockfile together with the manifest. Build and test afterward; use `npm ci` on fresh checkouts. Do not bypass compatibility conflicts with `--force` or `--legacy-peer-deps`.
- Publisher uses the same frontend dependency baseline and runs only for local maintainers, on development port 4201. Its build commands are the same.

## Next steps

- [Architecture](/docs/en-US/latest/concepts/architecture)
- [Application model](/docs/en-US/latest/concepts/application-model)
