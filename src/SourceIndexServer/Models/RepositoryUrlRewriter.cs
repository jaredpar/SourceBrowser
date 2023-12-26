
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration.UserSecrets;

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
        if (context.Request.Path.Value is string { Length: > 0 } path && path[0] == '/')
        {
            var index = path.IndexOf('/', startIndex: 1);
            if (index > 1)
            {
                var repositoryName = path.Substring(1, index - 1);
                if (Manager.TryGetRepository(repositoryName, out var repository))
                {
                    var newPath = $"/{repository.IndexName}/{path.Substring(index + 1)}";
                    context.Request.Path = newPath;
                }
            }
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