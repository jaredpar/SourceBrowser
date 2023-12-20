using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.SourceBrowser.SourceIndexServer;

var builder = WebApplication.CreateBuilder(args);

var rootPath = Path.Combine(builder.Environment.ContentRootPath, "index");
// HACK
var htmlGeneratorFilePath = @"C:\Users\jaredpar\code\SourceBrowser\src\HtmlGenerator\bin\Debug\net8.0\HtmlGenerator.dll";
builder.Services.AddSingleton(new RepositoryManager(rootPath));
builder.Services.AddSingleton<RepositoryUrlRewriter>();
builder.Services.AddScoped(sp => new ProjectContentGenerator(rootPath, htmlGeneratorFilePath));
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();
app.Use(async (context, next) =>
{
    context.Response.Headers["X-UA-Compatible"] = "IE=edge";
    await next();
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
