using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.SourceBrowser.SourceIndexServer;

var builder = WebApplication.CreateBuilder(args);

var rootPath = Path.Combine(builder.Environment.ContentRootPath, "index");
// HACK
var htmlGeneratorFilePath = (Util.InDocker, RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) switch
{
    (true, _) => @"/App/generator/HtmlGenerator.dll",
    (false, true) => @"C:\Users\jaredpar\code\SourceBrowser\src\HtmlGenerator\bin\Debug\net8.0\HtmlGenerator.dll",
    (false, false) =>"/home/jaredpar/code/SourceBrowser/src/HtmlGenerator/bin/Debug/net8.0/HtmlGenerator.dll",
};

builder.Services.AddSingleton(new RepositoryManager(rootPath));
builder.Services.AddSingleton<RepositoryUrlRewriter>();
builder.Services.AddScoped(sp => new RepositoryGenerator(rootPath, htmlGeneratorFilePath, sp.GetRequiredService<ILogger<RepositoryGenerator>>()));
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

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

var provider = new PhysicalFileProvider(rootPath, ExclusionFilters.Sensitive & ~ExclusionFilters.DotPrefixed);
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
