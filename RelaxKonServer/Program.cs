using Microsoft.AspNetCore.Diagnostics;
using RelaxKonServer.Options;
using RelaxKonServer.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddMemoryCache();
builder.Services.Configure<DocumentationOptions>(builder.Configuration.GetSection(DocumentationOptions.SectionName));
builder.Services.Configure<CorsOptions>(builder.Configuration.GetSection(CorsOptions.SectionName));
builder.Services.AddSingleton<IDocumentService, DocumentService>();
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
    await Results.Problem(statusCode: StatusCodes.Status500InternalServerError,
        title: "An unexpected server error occurred.").ExecuteAsync(context);
}));
if (!app.Environment.IsDevelopment()) app.UseHsts();
app.UseHttpsRedirection();
app.UseCors("development");
app.UseAuthorization();
app.MapGet("/swagger/v1/swagger.json", () => Results.Json(new
{
    openapi = "3.0.3",
    info = new { title = "RelaxKon Website API", version = "v1", description = "Public content API for the RelaxKon website." },
    paths = new Dictionary<string, object>
    {
        ["/api/docs/languages"] = new { get = new { summary = "List documentation languages" } },
        ["/api/docs/versions"] = new { get = new { summary = "List documentation versions" } },
        ["/api/docs/{language}/{version}/navigation"] = new { get = new { summary = "Get documentation navigation" } },
        ["/api/docs/{language}/{version}/{slug}"] = new { get = new { summary = "Get a Markdown document" } },
        ["/api/docs/search"] = new { get = new { summary = "Search documentation" } },
        ["/api/downloads"] = new { get = new { summary = "List downloads" } },
        ["/api/releases"] = new { get = new { summary = "List releases" } },
        ["/api/releases/{version}"] = new { get = new { summary = "Get a release" } },
        ["/api/faq"] = new { get = new { summary = "List FAQ items" } }
    }
}));
app.MapGet("/swagger", () => Results.Content("""
<!doctype html><html><head><title>RelaxKon Website API</title><link rel="stylesheet" href="https://unpkg.com/swagger-ui-dist/swagger-ui.css"></head><body><div id="swagger-ui"></div><script src="https://unpkg.com/swagger-ui-dist/swagger-ui-bundle.js"></script><script>SwaggerUIBundle({url:'/swagger/v1/swagger.json',dom_id:'#swagger-ui'});</script></body></html>
""", "text/html"));
app.MapControllers();
app.Run();
