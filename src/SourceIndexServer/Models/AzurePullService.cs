#nullable enable

using System.IO.Compression;
using Basic.Azure.Pipelines;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace Microsoft.SourceBrowser.SourceIndexServer;

public sealed class AzurePullService : BackgroundService
{
    public RepositoryManager RepositoryManager { get; } 
    public IConfiguration Configuration { get; }
    public IServiceProvider ServiceProvider { get; }

    public AzurePullService(RepositoryManager repositoryManager, IConfiguration configuration, IServiceProvider serviceProvider)
    {
        RepositoryManager = repositoryManager;
        Configuration = configuration;
        ServiceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await PullRepositories(cancellationToken);
            await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
        }
    }

    private async Task PullRepositories(CancellationToken cancellationToken)
    {
        using var scope = ServiceProvider.CreateScope();
        await PullRepositories(
            scope.ServiceProvider.GetRequiredService<IHttpClientFactory>(),
            scope.ServiceProvider.GetRequiredService<RepositoryGenerator>(),
            cancellationToken);
    }

    private async Task PullRepositories(IHttpClientFactory httpClientFactory, RepositoryGenerator generator, CancellationToken cancellationToken)
    {
        var buildId = 509211;
        var project = "public";
        var artifactName = "Windows_NT_Windows debug Attempt 1 Logs";

        var token = new AuthorizationToken(AuthorizationKind.PersonalAccessToken, Configuration[Constants.KeyAzdoToken]!);
        var server = new DevOpsServer("dnceng-public", token, httpClientFactory.CreateClient());
        var artifacts = await server.ListArtifactsAsync(project, buildId);
        var artifact = artifacts.FirstOrDefault(x => x.Name == artifactName);
        if (artifact is null)
        {
            return;
        }

        var stream = await server.DownloadArtifactAsync(project, buildId, artifact.Name);
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
        var entry = zip.GetEntry($"{artifactName}/Build.complog");
        if (entry is null)
        {
            return;
        }

        var indexName = await generator.Generate("razor", entry.Open());
        RepositoryManager.AddOrUpdateRepository("razor", indexName);
    }
}