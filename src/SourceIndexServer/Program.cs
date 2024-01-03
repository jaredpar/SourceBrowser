using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.SourceBrowser.SourceIndexServer;
using Azure.Identity;
using Microsoft.Build.Framework;
using Azure.Core;

var builder = WebApplication.CreateBuilder(args);
var inDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
var inAzure = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME"));
var rootPath = Path.Combine(builder.Environment.ContentRootPath, ".data");

Console.WriteLine($"In Docker: {inDocker}");
Console.WriteLine($"In Azure: {inAzure}");
Console.WriteLine($"In Production: {builder.Environment.IsProduction()}");
Console.WriteLine($"Root Path: {rootPath}");

if (builder.Environment.IsProduction())
{
    TokenCredential credential = (inDocker, inAzure) switch
    {
        (true, false) => new EnvironmentCredential(), // provided by --env-file
        (true, true) => new DefaultAzureCredential(), // provided by Azure managed id
        _ => throw new InvalidOperationException("Unsupported environment"),
    };

    builder.Configuration.AddAzureKeyVault(
        new Uri($"https://codenav.vault.azure.net/"),
        credential);
}

builder.Services.AddSingleton(sp => new RepositoryManager(rootPath, sp.GetRequiredService<ILogger<RepositoryManager>>()));
builder.Services.AddSingleton<RepositoryUrlRewriter>();
builder.Services.AddScoped<RepositoryGenerator>();
builder.Services.AddScoped<IGitHubClientFactory, GitHubClientFactory>();
builder.Services.AddHttpClient();
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddHostedService<ComplogUpdateService>();

var app = builder.Build();
app.Use(async (context, next) =>
{
    context.Response.Headers["X-UA-Compatible"] = "IE=edge";

    if (context.Request.Path.Value is "/upload")
    {
        context.Features.Get<IHttpMaxRequestBodySizeFeature>().MaxRequestBodySize = 100 * 1024 * 1024;
    }

    await next(context);
});

if (builder.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

var provider = new PhysicalFileProvider(
    app.Services.GetRequiredService<RepositoryManager>().IndexPath,
    ExclusionFilters.Sensitive & ~ExclusionFilters.DotPrefixed);
app.UseRepositoryUrlRewriter();
app.UseDefaultFiles();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = provider,
});
app.UseStaticFiles();

app.UseRouting();

app.MapRazorPages();
app.MapControllers();
app.Run();
