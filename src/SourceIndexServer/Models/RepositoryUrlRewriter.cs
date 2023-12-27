
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.SourceBrowser.SourceIndexServer.Pages;

namespace Microsoft.SourceBrowser.SourceIndexServer;

public sealed class RepositoryUrlRewriter : IMiddleware
{
    private RepositoryManager Manager { get; }

    public RepositoryUrlRewriter(RepositoryManager manager)
    {
        Manager = manager;
    }

    public Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.Request.Path.Value is string path)
        {
            if (path.Length > 1 && path[0] == '/')
            {
                var index = path.IndexOf('/', startIndex: 1);
                bool isStandard;
                if (index > 1)
                {
                    var folder = path.Substring(1, index - 1);
                    isStandard = folder switch {
                        "content" => true,
                        "lib" => true,
                        "api" => true,
                        _ => false,
                    };
                }
                else
                {
                    isStandard = path switch {
                        "/index.html" => true,
                        "/documentoutline.html" => true,
                        "/favicon.ico" => true,
                        "/header.html" => true,
                        "/overview.html" => true,
                        "/styles.css" => true,
                        "/scripts.js" => true,
                        _ => false,
                    };
                }

                if (isStandard)
                {
                    return next(context);
                }
            }

            if (path == "/")
            {
                path = $"/{Manager.CurrentRepositoryIndex.contentName}/index.html";
            }
            else
            {
                path = $"/{Manager.CurrentRepositoryIndex.contentName}{path}";
            }

            context.Request.Path = path;
        }

        return next(context);
    }
}

public static class RepositoryUrlRewriterExtensions
{
    public static IApplicationBuilder UseRepositoryUrlRewriter(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RepositoryUrlRewriter>();
    }
}