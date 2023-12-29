#nullable enable

using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
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
            var filePath = Path.Combine(RepositoryManager.RootPath, "source.json");
            if (!File.Exists(filePath))
            {
                Logger.LogError($"Missing source.json file at {filePath}");
                return [];
            }

            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var updateJson = await JsonSerializer.DeserializeAsync<UpdateJson>(stream, Options, cancellationToken);
            if (updateJson is null)
            {
                Logger.LogError($"No sources in {filePath}");
                return [];
            }

            var list = new List<ICompilerLogSource>();

            // HACK
            list.Add(new WorkflowCompilerLogSource(
                "complog",
                "jaredpar",
                "complog",
                "dotnet.yml",
                "windows.complog",
                "msbuild.complog"));

            if (updateJson.Files is not null)
            {
                foreach (var json in updateJson.Files)
                {
                    HandleFileSystem(json);
                }
            }

            if (updateJson.Pipelines is not null)
            {
                foreach (var json in updateJson.Pipelines)
                {
                    HandleAzure(json);
                }
            }

            return list;

            void HandleFileSystem(FileJson json)
            {
                if (string.IsNullOrEmpty(json.SourceName) ||
                    string.IsNullOrEmpty(json.FilePath))
                {
                    Logger.LogError("Bad file system source");
                    return;
                }

                list.Add(new FileSystemSource(json.SourceName, json.FilePath));
            }

            void HandleAzure(AzurePipelineJson json)
            {
                if (string.IsNullOrEmpty(json.SourceName) ||
                    string.IsNullOrEmpty(json.Organization) ||
                    string.IsNullOrEmpty(json.Project) ||
                    string.IsNullOrEmpty(json.ArtifactName) ||
                    string.IsNullOrEmpty(json.FileName))
                {
                    Logger.LogError("Bad azure source");
                    return;
                }

                list.Add(new AzureBuildCompilerLogSource(
                    Configuration,
                    json.SourceName,
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

    public async Task<bool> TryUpdateCompilerLogAsync(RepositoryManager manager, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        const int max = 100;
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var token = new AuthorizationToken(AuthorizationKind.PersonalAccessToken, configuration[Constants.KeyAzdoToken]!);
        var server = new DevOpsServer(organization, token, httpClientFactory.CreateClient());
        var existingKey = await manager.GetCompilerLogKey(SourceName, cancellationToken);

        var count = 0;
        await foreach (var build in server.EnumerateBuildsAsync(project, definitions: [definition], statusFilter: BuildStatus.Completed))
        {
            count++;
            if (count >= max)
            {
                break;
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

