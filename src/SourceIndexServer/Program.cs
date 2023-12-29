using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.SourceBrowser.SourceIndexServer;

var builder = WebApplication.CreateBuilder(args);

var rootPath = Path.Combine(builder.Environment.ContentRootPath, ".data");

// HACK
builder.Configuration[Constants.KeyHtmlGeneratorFilePath] = (Util.InDocker, RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) switch
{
    (true, _) => @"/App/generator/HtmlGenerator.dll",
    (false, true) => @"C:\Users\jaredpar\code\SourceBrowser\src\HtmlGenerator\bin\Debug\net8.0\HtmlGenerator.dll",
    (false, false) =>"/home/jaredpar/code/SourceBrowser/src/HtmlGenerator/bin/Debug/net8.0/HtmlGenerator.dll",
};

// HACK
builder.Configuration[Constants.KeyAzdoToken] = File.ReadAllText(@"c:\users\jaredpar\.tokens\SourceBrowser\azdo.txt").Trim();
builder.Configuration[Constants.KeyGitHubAppId] = File.ReadAllText(@"c:\users\jaredpar\.tokens\SourceBrowser\githubappid.txt").Trim();
builder.Configuration[Constants.KeyGitHubAppSecretKey] = File.ReadAllText(@"c:\users\jaredpar\.tokens\SourceBrowser\githubappsecretkey.txt").Trim();

builder.Services.AddSingleton(new RepositoryManager(rootPath));
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
