#nullable enable

using System.IO.Compression;
using Basic.Azure.Pipelines;

namespace Microsoft.SourceBrowser.SourceIndexServer;

public sealed class ComplogUpdateService : BackgroundService
{
    private readonly List<ICompilerLogSource> compilerLogSources = new();

    public RepositoryManager RepositoryManager { get; } 
    public IConfiguration Configuration { get; }
    public IServiceProvider ServiceProvider { get; }

    public ComplogUpdateService(RepositoryManager repositoryManager, IConfiguration configuration, IServiceProvider serviceProvider)
    {
        RepositoryManager = repositoryManager;
        Configuration = configuration;
        ServiceProvider = serviceProvider;

        // HACK
        compilerLogSources.Add(new FileSystemSource(
            "console",
            @"c:\users\jaredpar\temp\console\msbuild.complog"));
        compilerLogSources.Add(new FileSystemSource(
            "complog",
            @"c:\users\jaredpar\code\complog\build.complog"));

            /*
        compilerLogSources.Add(new AzureBuildCompilerLogSource(
            configuration,
            "razor",
            organization: "dnceng-public",
            buildId:509211,
            project: "public",
            artifactName:"Windows_NT_Windows debug Attempt 1 Logs",
            fileName: "Build.complog"));
            */
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await TryUpdateCompilerLogs(cancellationToken);
            await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
        }
    }

    private async Task TryUpdateCompilerLogs(CancellationToken cancellationToken)
    {
        using var scope = ServiceProvider.CreateScope();
        var clientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
        var any = false;
        foreach (var compilerLogSource in compilerLogSources)
        {
            try
            {
                if (await compilerLogSource.TryUpdateCompilerLogAsync(RepositoryManager, clientFactory, cancellationToken))
                {
                    any = true;
                }
            }
            catch
            {
                // HACK
                // ignored
            }
        }

        if (any)
        {
            try
            {
                var generator = scope.ServiceProvider.GetRequiredService<RepositoryGenerator>();
                var indexName = await generator.GenerateAsync();
                await RepositoryManager.ReplaceIndexAsync(indexName);
            }
            catch
            {
                // HACK
                // ignored
            }
        }
    }
}

internal interface ICompilerLogSource
{
    string SourceName { get; }
    Task<bool> TryUpdateCompilerLogAsync(RepositoryManager manager, IHttpClientFactory httpClientFactory, CancellationToken cancellationToken);
}

file sealed class FileSystemSource(
    string sourceName,
    string filePath) : ICompilerLogSource
{
    public string SourceName => sourceName;

    public async Task<bool> TryUpdateCompilerLogAsync(RepositoryManager manager, IHttpClientFactory httpClientFactory, CancellationToken cancellationToken)
    {
        if (!File.Exists(filePath))
        {
            return false;
        }

        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return await manager.ReplaceCompilerLogAsync(sourceName, fileStream, cancellationToken);
    }
}

file sealed class AzureBuildCompilerLogSource(
    IConfiguration configuration,
    string sourceName,
    string organization,
    string project,
    int buildId,
    string artifactName,
    string fileName) : ICompilerLogSource
{
    public string SourceName => sourceName;

    public async Task<bool> TryUpdateCompilerLogAsync(RepositoryManager manager, IHttpClientFactory httpClientFactory, CancellationToken cancellationToken)
    {
        var token = new AuthorizationToken(AuthorizationKind.PersonalAccessToken, configuration[Constants.KeyAzdoToken]!);
        var server = new DevOpsServer(organization, token, httpClientFactory.CreateClient());
        var artifacts = await server.ListArtifactsAsync(project, buildId);
        var artifact = artifacts.FirstOrDefault(x => x.Name == artifactName);
        if (artifact is null)
        {
            return false;
        }

        var stream = await server.DownloadArtifactAsync(project, buildId, artifact.Name);
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
        var entry = zip.GetEntry($"{artifactName}/{fileName}");
        if (entry is null)
        {
            return false;
        }

        return await manager.ReplaceCompilerLogAsync(sourceName, entry.Open(), cancellationToken);
    }
}
