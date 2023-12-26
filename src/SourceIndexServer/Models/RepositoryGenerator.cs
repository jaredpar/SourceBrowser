#nullable enable
namespace Microsoft.SourceBrowser.SourceIndexServer;

/// <summary>
/// This class is responsible for generating the html / js pages for the given
/// repository onto disk.
/// </summary>
public sealed class RepositoryGenerator(RepositoryManager repositoryManager, string htmlGeneratorFilePath, ILogger<RepositoryGenerator> logger)
{
    public RepositoryManager RepositoryManager { get; } = repositoryManager;

    /// <summary>
    /// The path to the html generator tool that is used to generate the repository
    /// content.
    /// </summary>
    public string HtmlGeneratorFilePath { get; } = htmlGeneratorFilePath;

    private string ScratchPath { get; } = Path.Combine(repositoryManager.RootPath, ".scratch");

    /// <summary>
    /// Generates the repository content and returns the index name that it was generated
    /// to.
    /// </summary>
    public async Task<string> Generate(string repositoryName, Stream complogStream)
    {
        var dirName = Guid.NewGuid().ToString();
        var dirPath = Path.Combine(RepositoryManager.IndexPath, dirName);
        if (Directory.Exists(dirPath))
        {
            throw new ArgumentException($"Directory {dirName} already exists");
        }

        var complogFilePath = await SaveToScratch(complogStream);
        try
        {
            await RunHtmlGenerator(repositoryName, complogFilePath, dirPath);
        }
        finally
        {
            File.Delete(complogFilePath);
        }

        return dirName;
    }

    private async Task RunHtmlGenerator(string repositoryName, string complogFilePath, string destDirectory)
    {
        string[] args =
        [
            "exec",
            HtmlGeneratorFilePath,
            complogFilePath,
            $"/name:{repositoryName}",
            $"/out:{destDirectory}"
        ];

        logger.LogInformation($"Generating {complogFilePath} to {destDirectory} with name {repositoryName}");
        var result = await ProcessUtil.RunAsync("dotnet", args, workingDirectory: RepositoryManager.IndexPath);
        if (!result.Succeeded)
        {
            logger.LogError($"Html generator failed: {result.StandardOut} {result.StandardError}");
            throw new Exception($"Html generator failed");
        }
    }

    private async Task<string> SaveToScratch(Stream stream)
    {
        Directory.CreateDirectory(ScratchPath);
        var filePath = Path.Combine(ScratchPath, $"{Guid.NewGuid()}.complog");
        using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await stream.CopyToAsync(fileStream);
        return filePath;
    }
}