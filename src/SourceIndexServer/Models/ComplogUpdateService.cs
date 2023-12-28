#nullable enable

using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Basic.Azure.Pipelines;
using Microsoft.SourceBrowser.SourceIndexServer.Json;

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

    public async Task<bool> TryUpdateCompilerLogAsync(RepositoryManager manager, IHttpClientFactory httpClientFactory, CancellationToken cancellationToken)
    {
        const int max = 100;
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
