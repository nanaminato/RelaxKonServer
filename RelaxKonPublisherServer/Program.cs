using System.Text;
using RelaxKon_Publisher.Hubs;
using RelaxKon_Publisher.Models;
using RelaxKon_Publisher.Services;

Console.OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://127.0.0.1:5112", "http://[::1]:5112");
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
builder.Services.Configure<PublisherPathsOptions>(builder.Configuration.GetSection("PublisherPaths"));
builder.Services.Configure<AndroidImportOptions>(builder.Configuration.GetSection("AndroidImport"));
builder.Services.AddSingleton<PublisherService>();
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(
        "http://localhost:4200", "http://127.0.0.1:4200", "http://[::1]:4200",
        "http://localhost:4201", "http://127.0.0.1:4201", "http://[::1]:4201")
    .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();
app.UseExceptionHandler(exceptionApp => exceptionApp.Run(async context =>
{
    var feature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
    context.Response.StatusCode = StatusCodes.Status400BadRequest;
    await context.Response.WriteAsJsonAsync(new { error = feature?.Error.Message ?? "发布任务失败。" });
}));
app.UseCors();
app.MapControllers();
app.MapHub<PublisherHub>("/hubs/publisher");
app.Run();
