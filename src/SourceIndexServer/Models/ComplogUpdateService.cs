#nullable enable

using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Basic.Azure.Pipelines;
using Microsoft.SourceBrowser.SourceIndexServer.Json;
using Octokit;
using FileMode = System.IO.FileMode;

namespace Microsoft.SourceBrowser.SourceIndexServer;

public sealed partial class ComplogUpdateService : BackgroundService
{
    public static readonly JsonSerializerOptions Options = new JsonSerializerOptions()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
    };

    public RepositoryManager RepositoryManager { get; } 
    public IConfiguration Configuration { get; }
    public IServiceProvider ServiceProvider { get; }
    public ILogger<ComplogUpdateService> Logger { get; }

    public ComplogUpdateService(RepositoryManager repositoryManager, IConfiguration configuration, IServiceProvider serviceProvider, ILogger<ComplogUpdateService> logger)
    {
        RepositoryManager = repositoryManager;
        Configuration = configuration;
        ServiceProvider = serviceProvider;
        Logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var compilerLogSources = await LoadCompilerLogSourcesAsync(cancellationToken);
        if (compilerLogSources.Count == 0)
        {
            Logger.LogInformation("No compiler log sources found");
            return;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            await TryUpdateCompilerLogs(compilerLogSources, cancellationToken);
            await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
        }
    }

    private async Task<List<ICompilerLogSource>> LoadCompilerLogSourcesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var filePath = Path.Combine(RepositoryManager.RootPath, "sources.json");
            if (!File.Exists(filePath))
            {
                Logger.LogError($"Missing source.json file at {filePath}");
                return [];
            }

            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var sourcesJson = await JsonSerializer.DeserializeAsync<SourcesJson>(stream, Options, cancellationToken);
            if (sourcesJson is null)
            {
                Logger.LogError($"No sources in {filePath}");
                return [];
            }

            var list = new List<ICompilerLogSource>();
            foreach (var sourceJson in sourcesJson.Sources)
            {
                if (sourceJson.Name.Length == 0)
                {
                    continue;
                }

                if (sourceJson.File is not null)
                {
                    HandleFileSystem(sourceJson.Name, sourceJson.File);
                }
                else if (sourceJson.Workflow is not null)
                {
                    HandleWorkflow(sourceJson.Name, sourceJson.Workflow);
                }
                else if (sourceJson.Pipeline is not null)
                {
                    HandlePipeline(sourceJson.Name, sourceJson.Pipeline);
                }
            }

            return list;

            void HandleFileSystem(string sourceName, FileJson json)
            {
                if (string.IsNullOrEmpty(json.FilePath))
                {
                    Logger.LogError($"Bad file system source: {sourceName}");
                    return;
                }

                list.Add(new FileSystemSource(sourceName, json.FilePath));
            }

            void HandleWorkflow(string sourceName, WorkflowJson json)
            {
                if (string.IsNullOrEmpty(json.Owner) ||
                    string.IsNullOrEmpty(json.Repo) ||
                    string.IsNullOrEmpty(json.WorkflowFileName) ||
                    string.IsNullOrEmpty(json.ArtifactName) ||
                    string.IsNullOrEmpty(json.FileName))
                {
                    Logger.LogError($"Bad workflow source: {sourceName}");
                    return;
                }

                list.Add(new WorkflowCompilerLogSource(
                    sourceName,
                    json.Owner,
                    json.Repo,
                    json.WorkflowFileName,
                    json.ArtifactName,
                    json.FileName));
            }

            void HandlePipeline(string sourceName, PipelineJson json)
            {
                if (string.IsNullOrEmpty(json.Organization) ||
                    string.IsNullOrEmpty(json.Project) ||
                    string.IsNullOrEmpty(json.ArtifactName) ||
                    string.IsNullOrEmpty(json.FileName))
                {
                    Logger.LogError($"Bad pipeline source: {sourceName}");
                    return;
                }

                list.Add(new AzureBuildCompilerLogSource(
                    Configuration,
                    sourceName,
                    json.Organization,
                    json.Project,
                    json.Definition,
                    json.ArtifactName,
                    json.FileName));
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load compiler log sources");
            return [];
        }
    }

    private async Task TryUpdateCompilerLogs(IList<ICompilerLogSource> compilerLogSources, CancellationToken cancellationToken)
    {
        using var scope = ServiceProvider.CreateScope();
        var any = false;
        foreach (var compilerLogSource in compilerLogSources)
        {
            try
            {
                if (await compilerLogSource.TryUpdateCompilerLogAsync(RepositoryManager, scope.ServiceProvider, cancellationToken))
                {
                    any = true;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Failed to update compiler log for {compilerLogSource.SourceName}");
            }
        }

        if (any)
        {
            try
            {
                var generator = scope.ServiceProvider.GetRequiredService<RepositoryGenerator>();
                var indexName = await generator.GenerateAsync(cancellationToken);
                await RepositoryManager.ReplaceIndexAsync(indexName);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to re-generate the repository");
            }
        }
    }
}

internal interface ICompilerLogSource
{
    string SourceName { get; }
    Task<bool> TryUpdateCompilerLogAsync(RepositoryManager manager, IServiceProvider serviceProvider, CancellationToken cancellationToken);
}

file sealed class FileSystemSource(
    string sourceName,
    string filePath) : ICompilerLogSource
{
    public string SourceName => sourceName;

    public async Task<bool> TryUpdateCompilerLogAsync(RepositoryManager manager, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (!File.Exists(filePath))
        {
            return false;
        }

        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var sha = SHA256.Create();
        var hash = sha.ComputeHash(fileStream);
        var hashText = GetHashText(hash);
        fileStream.Position = 0;

        if (await manager.GetCompilerLogKey(SourceName, cancellationToken) is string existingKey &&
            existingKey == hashText)
        {
            return false;
        }

        await manager.ReplaceCompilerLogAsync(sourceName, hashText, fileStream, cancellationToken);
        return true;

        static string GetHashText(ReadOnlySpan<byte> span)
        {
            var builder = new StringBuilder();
            foreach (var b in span)
            {
                builder.Append($"{b:X2}");
            }

            return builder.ToString();
        }
    }
}

file sealed class AzureBuildCompilerLogSource(
    IConfiguration configuration,
    string sourceName,
    string organization,
    string project,
    int definition,
    string artifactName,
    string fileName) : ICompilerLogSource
{
    public string SourceName => sourceName;
    public HashSet<(int BuildNumber, DateTimeOffset Finished)> VisitedBuilds = new();

    public async Task<bool> TryUpdateCompilerLogAsync(RepositoryManager manager, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        LimitVisitedBuilds();
        const int max = 100;
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var token = new AuthorizationToken(AuthorizationKind.PersonalAccessToken, configuration[Constants.KeyAzdoToken]!);
        var server = new DevOpsServer(organization, token, httpClientFactory.CreateClient());
        var existingKey = await manager.GetCompilerLogKey(SourceName, cancellationToken);

        var count = 0;
        await foreach (var build in server.EnumerateBuildsAsync(project, definitions: [definition], statusFilter: BuildStatus.Completed))
        {
            cancellationToken.ThrowIfCancellationRequested();

            count++;
            if (count >= max)
            {
                break;
            }

            // Don't revisit builds that we've already seen. Builds can finish multiple times (different attempts) so the 
            // finish time needs to be part of the key.
            if (build.GetFinishTime() is DateTimeOffset finishTime)
            {
                if (!VisitedBuilds.Add((build.Id, finishTime)))
                {
                    continue;
                }
            }

            // If this is the last build that we got the compiler log from then no need to go any further.
            var key = build.GetBuildKey().ToString();
            if (key == existingKey)
            {
                break;
            }

            var artifacts = await server.ListArtifactsAsync(project, build.Id);
            var artifact = artifacts.FirstOrDefault(x => x.Name == artifactName);
            if (artifact is null)
            {
                continue;
            }

            var stream = await server.DownloadArtifactAsync(project, build.Id, artifact.Name);
            using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
            var entry = zip.GetEntry($"{artifactName}/{fileName}");
            if (entry is null)
            {
                continue;
            }

            await manager.ReplaceCompilerLogAsync(sourceName, key, entry.Open(), cancellationToken);
            return true;
        }

        return false;
    }

    private void LimitVisitedBuilds()
    {
        const int max = 200;
        if (VisitedBuilds.Count >= max)
        {
            var builds = VisitedBuilds.OrderByDescending(x => x.Finished).Take(max / 2);
            VisitedBuilds = new(builds);
        }
    }
}


file sealed class WorkflowCompilerLogSource(
    string sourceName,
    string owner,
    string repo,
    string workflowFileName,
    string artifactName,
    string fileName) : ICompilerLogSource
{
    public string SourceName => sourceName;

    public async Task<bool> TryUpdateCompilerLogAsync(RepositoryManager manager, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var gitHubClientFactory = serviceProvider.GetRequiredService<IGitHubClientFactory>();
        var gitHubClient = await gitHubClientFactory.CreateForAppAsync(owner, repo);
        var request = new WorkflowRunsRequest()
        {
            ExcludePullRequests = true,
            Status = CheckRunStatusFilter.Completed,
        };

        const int max = 50;
        int count = 0;
        var response = await gitHubClient.Actions.Workflows.Runs.ListByWorkflow(owner, repo, workflowFileName, request);
        foreach (var workflow in response.WorkflowRuns)
        {
            count++;
            if (count >= max)
            {
                break;
            }

            var artifacts = await gitHubClient.Actions.Artifacts.ListWorkflowArtifacts(owner, repo, workflow.Id, new ListArtifactsRequest()
            {
                Name = artifactName,
            });

            if (artifacts.Artifacts.Count != 1)
            {
                continue;
            }

            var key = workflow.Id.ToString();
            if (await manager.GetCompilerLogKey(SourceName, cancellationToken) is string existingKey &&
                existingKey == key)
            {
                return false;
            }

            var artifact = artifacts.Artifacts[0];
            var stream = await gitHubClient.Actions.Artifacts.DownloadArtifact(owner, repo, artifact.Id, "zip");
            using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
            var entry = zip.GetEntry(fileName);
            if (entry is null)
            {
                continue;
            }
            await manager.ReplaceCompilerLogAsync(SourceName, key, entry.Open(), cancellationToken);
            return true;
        }

        return false;
    }
}

