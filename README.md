# RelaxKon website API

`RelaxKonServer/RelaxKonServer` is a pure ASP.NET Core 10 REST API for the RelaxKon website. It does not host Razor pages, MVC views, static website files, or the Angular client.

## Run locally

- Install the .NET 10 SDK.
- From this directory, run `dotnet restore` and `dotnet watch run`.
- The HTTPS launch profile uses `https://localhost:7252`; inspect `RelaxKonServer/Properties/launchSettings.json` if that changes.
- If needed, trust the development certificate with `dotnet dev-certs https --trust`.

The API OpenAPI document is available at `/swagger/v1/swagger.json`. A Swagger UI entry point is available at `/swagger` (it loads the standard Swagger UI asset from a CDN).

## Documentation content

Documentation lives under `RelaxKonServer/Content/Docs/{language}/{version}`. Markdown uses optional YAML-style front matter with `title`, `description`, `category`, and `order`. Add a language by adding its language directory; add a version by adding a version directory. The API discovers both without frontend file access.

`Documentation:RootPath` and its cache TTL are strongly typed options. Development CORS origins come only from `appsettings.Development.json`; they are never hard-coded in `Program.cs`.

## Security and boundary

The document service validates logical language, version, and slug segments, resolves paths from the controlled content root, and verifies paths remain under that root. It returns public API models rather than server paths. The Angular client talks only to the relative `/api` endpoint.

See [`../WEBSITE_ARCHITECTURE.md`](../WEBSITE_ARCHITECTURE.md) for all project boundary constraints.
