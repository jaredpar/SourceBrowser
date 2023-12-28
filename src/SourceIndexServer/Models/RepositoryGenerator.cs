#nullable enable
namespace Microsoft.SourceBrowser.SourceIndexServer;

/// <summary>
/// This class is responsible for generating the html / js pages for the given
/// repository onto disk.
/// </summary>
public sealed class RepositoryGenerator(RepositoryManager repositoryManager, IConfiguration configuration, ILogger<RepositoryGenerator> logger)
{
    public RepositoryManager RepositoryManager { get; } = repositoryManager;

    /// <summary>
    /// The path to the html generator tool that is used to generate the repository
    /// content.
    /// </summary>
    public string HtmlGeneratorFilePath { get; } = configuration[Constants.KeyHtmlGeneratorFilePath]!;

    /// <summary>
    /// Generates the repository contents and returns the index name that it was generated
    /// to.
    /// </summary>
    public async Task<string> GenerateAsync(CancellationToken cancellationToken)
    {
        var dirName = Guid.NewGuid().ToString();
        var dirPath = Path.Combine(RepositoryManager.IndexPath, dirName);
        if (Directory.Exists(dirPath))
        {
            throw new ArgumentException($"Directory {dirName} already exists");
        }

        await RepositoryManager.DoWithCompilerLogs(async compilerLogs =>
        {
            await RunHtmlGenerator(compilerLogs, dirPath);
        }, cancellationToken);

        return dirName;
    }

    private async Task RunHtmlGenerator(IEnumerable<string> complogFilePaths, string destDirectory)
    {
        string[] args =
        [
            "exec",
            HtmlGeneratorFilePath,
            .. complogFilePaths,
            $"/out:{destDirectory}"
        ];

        logger.LogInformation($"Generating to {destDirectory}");
        var result = await ProcessUtil.RunAsync("dotnet", args, workingDirectory: RepositoryManager.IndexPath);
        if (!result.Succeeded)
        {
            logger.LogError($"Html generator failed: {result.StandardOut} {result.StandardError}");
            throw new Exception($"Html generator failed");
        }
    }
}