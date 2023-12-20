using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.SourceBrowser.SourceIndexServer.Models;

var builder = WebApplication.CreateBuilder(args);

var rootPath = Path.Combine(builder.Environment.ContentRootPath, "index");
builder.Services.AddSingleton(new ProjectManager(rootPath));
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
