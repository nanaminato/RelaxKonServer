using Microsoft.AspNetCore.Diagnostics;
using RelaxKonServer.Options;
using RelaxKonServer.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddMemoryCache();
builder.Services.AddResponseCompression(options => options.EnableForHttps = true);
builder.Services.Configure<DocumentationOptions>(builder.Configuration.GetSection(DocumentationOptions.SectionName));
builder.Services.Configure<ContentOptions>(builder.Configuration.GetSection(ContentOptions.SectionName));
builder.Services.Configure<CorsOptions>(builder.Configuration.GetSection(CorsOptions.SectionName));
builder.Services.AddSingleton<IDocumentService, DocumentService>();
builder.Services.AddSingleton<IReleaseService, ReleaseService>();
builder.Services.AddSingleton<IFaqService, FaqService>();
builder.Services.AddSingleton<IDownloadService, DownloadService>();
builder.Services.AddCors(options => options.AddPolicy("development", policy =>
{
    var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    if (origins.Length > 0) policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
}));

var app = builder.Build();

app.UseExceptionHandler(exceptionApp => exceptionApp.Run(async context =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    app.Logger.LogError(exception, "Unhandled API exception.");
    await Results.Problem(
        statusCode: StatusCodes.Status500InternalServerError,
        title: "An unexpected server error occurred.").ExecuteAsync(context);
}));

if (!app.Environment.IsDevelopment()) app.UseHsts();
app.UseHttpsRedirection();
app.UseResponseCompression();
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "no-referrer";
    headers["X-Permitted-Cross-Domain-Policies"] = "none";
    headers["Cross-Origin-Resource-Policy"] = "same-origin";
    // This service only ever returns JSON, so nothing should be loadable from a
    // response and no browser feature needs to be granted to it.
    headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'";
    headers["Permissions-Policy"] = "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()";
    await next();
});
app.UseCors("development");
app.UseAuthorization();

app.MapGet("/api/health", () => Results.Ok(new { status = "healthy", service = "RelaxKon Website API", utc = DateTimeOffset.UtcNow }));

app.MapGet("/swagger/v1/swagger.json", () => Results.Json(new
{
    openapi = "3.0.3",
    info = new
    {
        title = "RelaxKon Website API",
        version = "v1",
        description = "Public content API for the RelaxKon website. Documentation, releases, downloads and FAQ are served from Markdown and JSON content owned by RelaxKonServer."
    },
    paths = new Dictionary<string, object>
    {
        ["/api/health"] = new { get = new { summary = "Liveness probe", tags = new[] { "System" } } },
        ["/api/docs/languages"] = new { get = new { summary = "List documentation languages", tags = new[] { "Documentation" } } },
        ["/api/docs/versions"] = new { get = new { summary = "List documentation versions", tags = new[] { "Documentation" } } },
        ["/api/docs/{language}/{version}/navigation"] = new { get = new { summary = "Get the documentation navigation tree", tags = new[] { "Documentation" } } },
        ["/api/docs/{language}/{version}/{slug}"] = new { get = new { summary = "Get a Markdown document with previous/next links and headings", tags = new[] { "Documentation" } } },
        ["/api/docs/search"] = new { get = new { summary = "Search documentation titles and content", tags = new[] { "Documentation" } } },
        ["/api/downloads"] = new { get = new { summary = "List product downloads", tags = new[] { "Content" } } },
        ["/api/releases"] = new { get = new { summary = "List releases", tags = new[] { "Content" } } },
        ["/api/releases/{version}"] = new { get = new { summary = "Get a single release note", tags = new[] { "Content" } } },
        ["/api/faq"] = new { get = new { summary = "List FAQ entries for a language", tags = new[] { "Content" } } }
    }
}));
app.MapGet("/swagger", () => Results.Content("""
<!doctype html><html lang="en"><head><meta charset="utf-8"><title>RelaxKon Website API</title>
<meta name="viewport" content="width=device-width, initial-scale=1">
<link rel="stylesheet" href="https://unpkg.com/swagger-ui-dist/swagger-ui.css"></head>
<body><div id="swagger-ui"></div>
<script src="https://unpkg.com/swagger-ui-dist/swagger-ui-bundle.js"></script>
<script>SwaggerUIBundle({ url: '/swagger/v1/swagger.json', dom_id: '#swagger-ui', deepLinking: true });</script>
</body></html>
""", "text/html"));

app.MapControllers();
app.Run();
